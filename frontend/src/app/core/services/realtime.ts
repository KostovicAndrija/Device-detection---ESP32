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

@Injectable({ providedIn: 'root' })
export class Realtime {
  private readonly auth = inject(AuthService);
  private connection?: HubConnection;

  readonly devices = signal<RealtimeDeviceState[]>([]);
  readonly alerts = signal<Alert[]>([]);
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
    this.connection.on('alertCreated', event => this.alerts.update(current => [
      {
        id: event.alertId,
        deviceId: event.deviceHash,
        sessionId: event.sessionId,
        riskScore: event.riskScore,
        reason: event.reason,
        createdAt: event.createdAt,
        acknowledgedAt: null
      },
      ...current
    ]));
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
    this.selectedSession.set(null);
    await this.connection?.stop();
    this.connectionStatus.set('disconnected');
  }

  setInitialAlerts(alerts: Alert[]): void {
    this.alerts.set(alerts);
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
