import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api, ExamSession, SessionReport } from '../../../../core/services/api';

@Component({ selector: 'app-reports', imports: [FormsModule, DatePipe], templateUrl: './reports.html', styleUrl: './reports.scss' })
export class Reports implements OnInit {
  private readonly api = inject(Api);
  readonly sessions = signal<ExamSession[]>([]);
  readonly report = signal<SessionReport | null>(null);
  sessionId = '';
  ngOnInit(): void { this.api.sessions().subscribe(x => { this.sessions.set(x); this.sessionId = x[0]?.id ?? ''; this.load(); }); }
  load(): void { if (this.sessionId) this.api.report(this.sessionId).subscribe(x => this.report.set(x)); }
  exportCsv(): void {
    const report = this.report(); if (!report) return;
    const csv = `sessionId,observations,alerts,devices,generatedAt\n${report.sessionId},${report.observationCount},${report.alertCount},${report.distinctDeviceCount},${report.generatedAt}\n`;
    const link = document.createElement('a');
    link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
    link.download = `session-${report.sessionId}.csv`; link.click(); URL.revokeObjectURL(link.href);
  }
}
