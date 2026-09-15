import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface ExamSession {
  id: string;
  name: string;
  roomId: string;
  startsAt: string;
  endsAt: string | null;
  status: 'planned' | 'active' | 'completed';
}

export interface Device {
  id: string;
  hashId: string;
  type: string;
  firstSeenAt: string;
  lastSeenAt: string;
}

export interface Alert {
  id: string;
  deviceId: string;
  sessionId: string | null;
  riskScore: number;
  reason: string;
  createdAt: string;
  acknowledgedAt: string | null;
}

export interface WhitelistEntry {
  staffDeviceId?: string | null;
  id: string;
  sessionId: string;
  studentRef: string;
  deviceHash: string;
  validFrom: string;
  validTo: string | null;
}

export interface DevicePosition {
  deviceId: string;
  x: number;
  y: number;
  confidence: number;
  capturedAt: string;
  sensorCount: number;
  isWhitelisted: boolean;
}

export interface SessionReport {
  sessionId: string;
  observationCount: number;
  alertCount: number;
  distinctDeviceCount: number;
  generatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);
  readonly restBaseUrl = '/api';
  readonly signalRHubUrl = '/hubs/monitoring';

  sessions(): Observable<ExamSession[]> {
    return this.http.get<ExamSession[]>(`${this.restBaseUrl}/sessions`);
  }

  createSession(request: { name: string; roomId: string; startsAt: string }): Observable<ExamSession> {
    return this.http.post<ExamSession>(`${this.restBaseUrl}/sessions`, request);
  }

  startSession(id: string): Observable<ExamSession> {
    return this.http.post<ExamSession>(`${this.restBaseUrl}/sessions/${id}/start`, {});
  }

  startRoomSession(roomId: string): Observable<ExamSession> {
    return this.http.post<ExamSession>(`${this.restBaseUrl}/sessions/start-for-room`, { roomId });
  }

  stopSession(id: string): Observable<ExamSession> {
    return this.http.post<ExamSession>(`${this.restBaseUrl}/sessions/${id}/stop`, {});
  }

  activeDevices(windowMinutes = 10): Observable<Device[]> {
    return this.http.get<Device[]>(`${this.restBaseUrl}/devices/active`, { params: { windowMinutes } });
  }

  alerts(sessionId: string): Observable<Alert[]> {
    return this.http.get<Alert[]>(`${this.restBaseUrl}/sessions/${sessionId}/alerts`);
  }

  acknowledgeAlert(id: string): Observable<void> {
    return this.http.post<void>(`${this.restBaseUrl}/alerts/${id}/acknowledge`, {});
  }

  positions(sessionId: string): Observable<DevicePosition[]> {
    return this.http.get<DevicePosition[]>(`${this.restBaseUrl}/sessions/${sessionId}/positions`);
  }

  whitelist(sessionId: string): Observable<WhitelistEntry[]> {
    return this.http.get<WhitelistEntry[]>(`${this.restBaseUrl}/sessions/${sessionId}/whitelist`);
  }

  addWhitelist(sessionId: string, request: {
    studentRef: string;
    deviceIdentifier: string;
    validFrom: string;
    validTo: string | null;
  }): Observable<WhitelistEntry> {
    return this.http.post<WhitelistEntry>(`${this.restBaseUrl}/sessions/${sessionId}/whitelist`, request);
  }

  removeWhitelist(sessionId: string, entryId: string): Observable<void> {
    return this.http.delete<void>(`${this.restBaseUrl}/sessions/${sessionId}/whitelist/${entryId}`);
  }

  report(sessionId: string): Observable<SessionReport> {
    return this.http.get<SessionReport>(`${this.restBaseUrl}/reports/session/${sessionId}`);
  }
}
