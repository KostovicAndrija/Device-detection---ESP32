import { DecimalPipe, UpperCasePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CLASSROOMS, classroomById } from '../../../../core/classrooms';
import { Api, DevicePosition, ExamSession } from '../../../../core/services/api';

interface Desk { id: number; left: number; top: number; }

@Component({ selector: 'app-floor-map', imports: [FormsModule, DecimalPipe, UpperCasePipe], templateUrl: './floor-map.html', styleUrl: './floor-map.scss' })
export class FloorMap implements OnInit, OnDestroy {
  private readonly api = inject(Api);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private refreshTimer?: ReturnType<typeof setInterval>;
  private clockTimer?: ReturnType<typeof setInterval>;
  readonly classrooms = CLASSROOMS;
  readonly sessions = signal<ExamSession[]>([]);
  readonly positions = signal<DevicePosition[]>([]);
  readonly clock = signal(Date.now());
  readonly loading = signal(false);
  readonly error = signal('');
  readonly lastRefresh = signal<Date | null>(null);
  readonly visiblePositions = computed(() => this.positions().filter(position => this.ageMs(position, this.clock()) < 45_000));
  readonly activeCount = computed(() => this.positions().filter(position => this.ageMs(position, this.clock()) < 8_000).length);
  readonly fadingCount = computed(() => this.visiblePositions().length - this.activeCount());
  readonly selectedSession = computed(() => this.sessions().find(session => session.id === this.sessionId));
  readonly selectedClassroom = computed(() => classroomById(this.roomId));
  readonly desks = computed<Desk[]>(() => {
    if (this.selectedClassroom().layout === 'aisle-2-3') {
      const columns = [12, 27, 55, 70, 85], rows = [27, 40, 53, 66, 79];
      return rows.flatMap((top, row) => columns.map((left, column) => ({ id: row * 5 + column + 1, left, top })));
    }
    const columns = this.selectedClassroom().layout === 'compact' ? [24, 50, 76] : [18, 40, 62, 84];
    const rows = this.selectedClassroom().layout === 'compact' ? [34, 56, 78] : [28, 49, 70];
    return rows.flatMap((top, row) => columns.map((left, column) => ({ id: row * columns.length + column + 1, left, top })));
  });
  sessionId = '';
  roomId = CLASSROOMS[0].id;

  ngOnInit(): void {
    const requestedRoom = this.route.snapshot.queryParamMap.get('room');
    const requestedSession = this.route.snapshot.queryParamMap.get('session');
    if (requestedRoom) this.roomId = requestedRoom;
    this.api.sessions().subscribe(sessions => {
      this.sessions.set(sessions);
      const linked = sessions.find(session => session.id === requestedSession);
      if (linked) this.roomId = linked.roomId;
      this.sessionId = linked?.id ?? sessions.find(session => session.roomId === this.roomId && session.status === 'active')?.id ?? '';
      this.load();
    });
    this.refreshTimer = setInterval(() => this.load(false), 2_000);
    this.clockTimer = setInterval(() => this.clock.set(Date.now()), 1_000);
  }
  ngOnDestroy(): void { if (this.refreshTimer) clearInterval(this.refreshTimer); if (this.clockTimer) clearInterval(this.clockTimer); }
  load(showSpinner = true): void {
    if (!this.sessionId) { this.loading.set(false); return; }
    if (showSpinner) this.loading.set(true);
    this.api.positions(this.sessionId).subscribe({
      next: positions => { this.positions.set(positions); this.lastRefresh.set(new Date()); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
  selectSession(): void {
    const selected = this.sessions().find(session => session.id === this.sessionId);
    if (selected) this.roomId = selected.roomId;
    this.positions.set([]); this.updateUrl(); this.load();
  }
  selectRoom(): void {
    this.sessionId = this.sessions().find(session => session.roomId === this.roomId && session.status === 'active')?.id ?? '';
    this.positions.set([]); this.updateUrl(); this.load();
  }
  startReading(): void {
    this.error.set('');
    this.loading.set(true);
    this.api.startRoomSession(this.roomId).subscribe({
      next: session => { this.sessions.update(items => [session, ...items]); this.sessionId = session.id; this.updateUrl(); this.load(); },
      error: e => { this.error.set(e.error?.title ?? 'Očitavanje nije moguće pokrenuti.'); this.loading.set(false); this.reloadSessions(); }
    });
  }
  stopReading(): void {
    const session = this.selectedSession();
    if (!session || session.status !== 'active') return;
    this.api.stopSession(session.id).subscribe(updated => {
      this.sessions.update(items => items.map(item => item.id === updated.id ? updated : item)); this.positions.set([]);
    });
  }
  left(x: number): string { return `${Math.max(3, Math.min(97, x / 8 * 100))}%`; }
  top(y: number): string { return `${Math.max(8, Math.min(92, y / 6 * 100))}%`; }
  opacity(position: DevicePosition): number { const age = this.ageMs(position, this.clock()); return age <= 8_000 ? 1 : Math.max(0, 1 - (age - 8_000) / 37_000); }
  presence(position: DevicePosition): 'live' | 'fading' { return this.ageMs(position, this.clock()) < 8_000 ? 'live' : 'fading'; }
  shortId(deviceId: string): string { return deviceId.slice(0, 4).toUpperCase(); }
  private reloadSessions(): void { this.api.sessions().subscribe(sessions => { this.sessions.set(sessions); this.sessionId = sessions.find(s => s.roomId === this.roomId && s.status === 'active')?.id ?? ''; this.loading.set(false); this.updateUrl(); }); }
  private updateUrl(): void { this.router.navigate([], { relativeTo: this.route, queryParams: { room: this.roomId, session: this.sessionId || null }, replaceUrl: true }); }
  private ageMs(position: DevicePosition, now: number): number { return Math.max(0, now - new Date(position.capturedAt).getTime()); }
}
