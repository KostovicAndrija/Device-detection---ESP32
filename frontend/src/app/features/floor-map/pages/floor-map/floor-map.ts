import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api, DevicePosition, ExamSession } from '../../../../core/services/api';

@Component({ selector: 'app-floor-map', imports: [FormsModule, DecimalPipe], templateUrl: './floor-map.html', styleUrl: './floor-map.scss' })
export class FloorMap implements OnInit {
  private readonly api = inject(Api);
  readonly sessions = signal<ExamSession[]>([]);
  readonly positions = signal<DevicePosition[]>([]);
  sessionId = '';
  ngOnInit(): void { this.api.sessions().subscribe(x => { this.sessions.set(x); this.sessionId = x.find(s => s.status === 'active')?.id ?? x[0]?.id ?? ''; this.load(); }); }
  load(): void { if (this.sessionId) this.api.positions(this.sessionId).subscribe(x => this.positions.set(x)); }
  left(x: number): string { return `${Math.max(0, Math.min(100, x / 8 * 100))}%`; }
  top(y: number): string { return `${Math.max(0, Math.min(100, y / 6 * 100))}%`; }
}
