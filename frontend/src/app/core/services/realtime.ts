import { Injectable, computed, signal } from '@angular/core';

export interface RealtimeDeviceState {
  deviceHash: string;
  latestRssi: number;
}

@Injectable({
  providedIn: 'root',
})
export class Realtime {
  readonly devices = signal<RealtimeDeviceState[]>([]);
  readonly selectedSession = signal<string | null>(null);
  readonly activeAlertCount = signal(0);
  readonly connectionStatus = signal<'connected' | 'disconnected'>('disconnected');

  readonly connectedDevices = computed(() => this.devices().length);

  setConnectionStatus(status: 'connected' | 'disconnected'): void {
    this.connectionStatus.set(status);
  }

  upsertDevice(state: RealtimeDeviceState): void {
    this.devices.update(current => {
      const index = current.findIndex(device => device.deviceHash === state.deviceHash);

      if (index === -1) {
        return [...current, state];
      }

      const updated = [...current];
      updated[index] = state;
      return updated;
    });
  }
}
