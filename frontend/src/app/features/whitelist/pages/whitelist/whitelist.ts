import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api, ExamSession, WhitelistEntry } from '../../../../core/services/api';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({ selector: 'app-whitelist', imports: [FormsModule, DatePipe], templateUrl: './whitelist.html', styleUrl: './whitelist.scss' })
export class Whitelist implements OnInit {
  readonly auth = inject(AuthService);
  private readonly api = inject(Api);
  readonly sessions = signal<ExamSession[]>([]);
  readonly entries = signal<WhitelistEntry[]>([]);
  sessionId = ''; studentRef = ''; deviceIdentifier = ''; validFrom = new Date().toISOString().slice(0, 16); validTo = '';
  ngOnInit(): void { this.api.sessions().subscribe(x => { this.sessions.set(x); this.sessionId = x[0]?.id ?? ''; this.load(); }); }
  load(): void { if (this.sessionId) this.api.whitelist(this.sessionId).subscribe(x => this.entries.set(x)); }
  add(): void {
    this.api.addWhitelist(this.sessionId, {
      studentRef: this.studentRef, deviceIdentifier: this.deviceIdentifier,
      validFrom: new Date(this.validFrom).toISOString(), validTo: this.validTo ? new Date(this.validTo).toISOString() : null
    }).subscribe(entry => { this.entries.update(x => [...x, entry]); this.studentRef = ''; this.deviceIdentifier = ''; });
  }
  remove(entry: WhitelistEntry): void {
    this.api.removeWhitelist(this.sessionId, entry.id).subscribe(() => this.entries.update(x => x.filter(item => item.id !== entry.id)));
  }
}
