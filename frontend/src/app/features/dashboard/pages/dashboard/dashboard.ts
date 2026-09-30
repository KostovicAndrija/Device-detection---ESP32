import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Rooms, RoomLayout } from '../../../../core/services/rooms';
import { Api, ExamSession } from '../../../../core/services/api';
import { Realtime } from '../../../../core/services/realtime';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({ selector: 'app-dashboard', imports: [DatePipe], templateUrl: './dashboard.html', styleUrl: './dashboard.scss' })
export class Dashboard implements OnInit {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  readonly realtime = inject(Realtime);
  readonly auth = inject(AuthService);
  readonly roomStore = inject(Rooms);
  readonly classrooms = this.roomStore.items;
  readonly sessions = signal<ExamSession[]>([]);
  readonly loading = signal(false);
  readonly startingRoom = signal<string | null>(null);
  readonly error = signal('');
  readonly activeSession = computed(() => this.sessions().find(x => x.status === 'active') ?? null);

  ngOnInit(): void { this.roomStore.load(); this.load(); }
  load(): void {
    this.loading.set(true); this.error.set('');
    this.api.sessions().subscribe({
      next: sessions => {
        this.sessions.set(sessions);
        const active = sessions.find(x => x.status === 'active');
        this.realtime.selectedSession.set(active?.id ?? null);
        if (active) this.api.alerts(active.id).subscribe(x => this.realtime.setInitialAlerts(x));
        this.loading.set(false);
      },
      error: () => { this.error.set('Sesije nije moguće učitati.'); this.loading.set(false); }
    });
  }
  activeFor(roomId: string): ExamSession | undefined {
    return this.sessions().find(session => session.roomId === roomId && session.status === 'active');
  }
  startRoom(room: RoomLayout): void {
    const active = this.activeFor(room.id);
    if (active) { this.openRoom(room.id, active.id); return; }
    this.startingRoom.set(room.id); this.error.set('');
    this.api.startRoomSession(room.id).subscribe({
      next: session => {
        this.sessions.update(items => [session, ...items]);
        this.realtime.selectedSession.set(session.id); this.startingRoom.set(null);
        this.openRoom(room.id, session.id);
      },
      error: e => {
        this.error.set(e.error?.title ?? 'Očitavanje nije moguće pokrenuti.');
        this.startingRoom.set(null);
      }
    });
  }
  stop(session: ExamSession, event?: Event): void {
    event?.stopPropagation();
    this.api.stopSession(session.id).subscribe(result => {
      if (result.deleted) this.remove(session.id);
      else if (result.session) this.replace(result.session);
      if (this.realtime.selectedSession() === session.id) this.realtime.selectedSession.set(null);
    });
  }
  delete(session: ExamSession, event?: Event): void {
    event?.stopPropagation();
    if (!this.auth.isProfessor() || !confirm(`Obrisati sesiju „${session.name}“ i sva njena očitavanja i alarme?`)) return;
    this.api.deleteSession(session.id).subscribe({
      next: () => this.remove(session.id),
      error: e => this.error.set(e.error?.title ?? 'Sesiju nije moguće obrisati.')
    });
  }
  openSession(session: ExamSession): void { this.openRoom(session.roomId, session.id); }
  openRoom(roomId: string, sessionId?: string): void {
    this.router.navigate(['/floor-map'], { queryParams: { room: roomId, session: sessionId ?? null } });
  }
  private replace(updated: ExamSession): void {
    this.sessions.update(items => items.map(item => item.id === updated.id ? updated : item));
  }
  private remove(id: string): void {
    this.sessions.update(items => items.filter(item => item.id !== id));
    if (this.realtime.selectedSession() === id) this.realtime.selectedSession.set(null);
  }
}
