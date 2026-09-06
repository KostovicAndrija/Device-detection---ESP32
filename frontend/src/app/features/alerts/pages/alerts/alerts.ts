import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Alert, Api, ExamSession } from '../../../../core/services/api';

@Component({ selector: 'app-alerts', imports: [FormsModule, DatePipe], templateUrl: './alerts.html', styleUrl: './alerts.scss' })
export class Alerts implements OnInit {
  private readonly api = inject(Api);
  readonly sessions = signal<ExamSession[]>([]);
  readonly alerts = signal<Alert[]>([]);
  sessionId = '';
  ngOnInit(): void {
    this.api.sessions().subscribe(items => {
      this.sessions.set(items);
      this.sessionId = items.find(x => x.status === 'active')?.id ?? items[0]?.id ?? '';
      this.load();
    });
  }
  load(): void { if (this.sessionId) this.api.alerts(this.sessionId).subscribe(x => this.alerts.set(x)); }
  acknowledge(alert: Alert): void {
    this.api.acknowledgeAlert(alert.id).subscribe(() => this.alerts.update(items =>
      items.map(x => x.id === alert.id ? { ...x, acknowledgedAt: new Date().toISOString() } : x)));
  }
  severity(score: number): string { return score >= 85 ? 'high' : score >= 70 ? 'medium' : 'low'; }
}
