import { Injectable, computed, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AuthService } from '../auth/auth.service';
import { Alert } from './api';

export interface RealtimeDeviceState {
  deviceHash: string;
  sensorId: string;
  sessionId: string | null;
  latestRssi: number;
  capturedAt: string;
  riskScore: number;
}

export interface AlertToast {
  id: string;
  signalType: string;
  riskScore: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class Realtime {
  private readonly auth = inject(AuthService);
  private connection?: HubConnection;

  readonly devices = signal<RealtimeDeviceState[]>([]);
  readonly alerts = signal<Alert[]>([]);
  readonly toasts = signal<AlertToast[]>([]);
  readonly selectedSession = signal<string | null>(null);
  readonly connectionStatus = signal<'connecting' | 'connected' | 'disconnected'>('disconnected');
  readonly connectedDevices = computed(() => this.devices().length);
  readonly activeAlertCount = computed(() => this.alerts().filter(x => !x.acknowledgedAt).length);

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected ||
        this.connection?.state === HubConnectionState.Connecting) {
      return;
    }

    this.connectionStatus.set('connecting');
    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/monitoring', { accessTokenFactory: () => this.auth.accessToken() ?? '' })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    this.connection.on('deviceUpdated', event => this.upsertDevice({
      deviceHash: event.deviceHash,
      sensorId: event.sensorId,
      sessionId: event.sessionId,
      latestRssi: event.rssi,
      capturedAt: event.capturedAt,
      riskScore: event.riskScore
    }));
    this.connection.on('alertCreated', event => this.handleAlertCreated(event));
    this.connection.on('alertAcknowledged', event => this.alerts.update(current =>
      current.map(item => item.id === event.alertId
        ? { ...item, acknowledgedAt: event.acknowledgedAt }
        : item)
    ));
    this.connection.onreconnecting(() => this.connectionStatus.set('connecting'));
    this.connection.onreconnected(() => this.connectionStatus.set('connected'));
    this.connection.onclose(() => this.connectionStatus.set('disconnected'));

    try {
      await this.connection.start();
      this.connectionStatus.set('connected');
    } catch {
      this.connectionStatus.set('disconnected');
    }
  }

  async disconnect(): Promise<void> {
    this.devices.set([]);
    this.alerts.set([]);
    this.toasts.set([]);
    this.selectedSession.set(null);
    await this.connection?.stop();
    this.connectionStatus.set('disconnected');
  }

  setInitialAlerts(alerts: Alert[]): void {
    this.alerts.set(alerts);
  }

  dismissToast(id: string): void {
    this.toasts.update(current => current.filter(x => x.id !== id));
  }

  private handleAlertCreated(event: { alertId: string; deviceHash: string; sessionId: string | null; riskScore: number; signalType: string; reason: string; createdAt: string }): void {
    if (this.alerts().some(x => x.id === event.alertId)) return;
    const alert: Alert = {
      id: event.alertId,
      deviceId: event.deviceHash,
      sessionId: event.sessionId,
      riskScore: event.riskScore,
      signalType: event.signalType,
      reason: event.reason,
      createdAt: event.createdAt,
      acknowledgedAt: null
    };
    this.alerts.update(current => [alert, ...current]);
    this.toasts.update(current => [{ id: alert.id, signalType: alert.signalType, riskScore: alert.riskScore, createdAt: alert.createdAt }, ...current].slice(0, 4));
    setTimeout(() => this.dismissToast(alert.id), 10_000);
  }

  private upsertDevice(state: RealtimeDeviceState): void {
    if (this.selectedSession() && state.sessionId !== this.selectedSession()) {
      return;
    }
    this.devices.update(current => {
      const index = current.findIndex(device => device.deviceHash === state.deviceHash);
      if (index === -1) return [state, ...current];
      const updated = [...current];
      updated[index] = state;
      return updated;
    });
  }
}
