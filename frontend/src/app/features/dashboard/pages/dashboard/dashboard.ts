import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api, ExamSession } from '../../../../core/services/api';
import { Realtime } from '../../../../core/services/realtime';

@Component({
  selector: 'app-dashboard',
  imports: [FormsModule, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  private readonly api = inject(Api);
  readonly realtime = inject(Realtime);
  readonly sessions = signal<ExamSession[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly activeSession = computed(() => this.sessions().find(x => x.status === 'active') ?? null);
  name = 'Ispit';
  roomId = 'Ucionica-1';
  startsAt = new Date().toISOString().slice(0, 16);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
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

  create(): void {
    this.api.createSession({ name: this.name, roomId: this.roomId, startsAt: new Date(this.startsAt).toISOString() })
      .subscribe({ next: session => this.sessions.update(x => [session, ...x]), error: () => this.error.set('Kreiranje nije uspelo.') });
  }

  start(session: ExamSession): void {
    this.api.startSession(session.id).subscribe(updated => { this.replace(updated); this.realtime.selectedSession.set(updated.id); });
  }

  stop(session: ExamSession): void {
    this.api.stopSession(session.id).subscribe(updated => { this.replace(updated); this.realtime.selectedSession.set(null); });
  }

  private replace(updated: ExamSession): void {
    this.sessions.update(items => items.map(item => item.id === updated.id ? updated : item));
  }
}
