import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { CLASSROOMS } from '../../core/classrooms';

interface StaffDevice { id: string; label: string; deviceHash: string; }
interface Scan { id: string; status: string; registrationExpiresAt: string; }
interface Candidate { id: string; hashId: string; type: string; rssi: number; }

@Component({
  selector: 'app-staff', imports: [FormsModule, DatePipe],
  template: `
    <section class="page">
      <h2>Moji uređaji</h2>
      <p>Registrujte svoje uređaje pre praćenja. Sistem ih automatski stavlja na listu dozvoljenih uređaja svake sesije koju pokrenete.</p>
      @if (error()) { <p role="alert">{{error()}}</p> }
      @if (notice()) { <p role="status">{{notice()}}</p> }
      <ul>@for (device of devices(); track device.id) {
        <li>{{device.label}} · {{device.deviceHash.slice(0, 16)}}
          <button [disabled]="busy() || !!scan()" (click)="remove(device.id)">Ukloni</button></li>
      } @empty { <li>Još nemate registrovane uređaje.</li> }</ul>
      <div class="panel">
        <h3>Registraciono skeniranje</h3>
        <p>Budite sami u učionici, uključite svoje uređaje i pokrenite skeniranje. Skeniranje traje najviše dva minuta i ne pravi alarme. Potvrdite samo uređaje za koje znate da su vaši. Mogu se pojaviti i uređaji iz susednih prostorija.</p>
        @if (!scan()) {
          <label>Učionica <select [(ngModel)]="roomId">@for (room of rooms; track room.id) { <option [value]="room.id">{{room.name}}</option> }</select></label>
          <button [disabled]="busy()" (click)="start()">Skeniraj moje uređaje</button>
        } @else {
          <p>Prikupljanje očitavanja do {{scan()!.registrationExpiresAt | date:'mediumTime'}}. Nakon toga imate deset minuta da potvrdite izbor.</p>
          <p>Senzori moraju slati očitavanja za ovu sesiju: <code>{{scan()!.id}}</code>.</p>
          @for (candidate of candidates(); track candidate.id) {
            <div class="candidate">
              <label><input type="checkbox" [(ngModel)]="selected[candidate.id]" /> {{candidate.type}} · {{candidate.hashId.slice(0, 16)}} · {{candidate.rssi}} dBm</label>
              @if (selected[candidate.id]) { <input aria-label="Naziv uređaja" placeholder="Npr. moj telefon" maxlength="100" [(ngModel)]="labels[candidate.id]" /> }
            </div>
          } @empty { <p>Čekamo očitavanja senzora. Uključite bežičnu komunikaciju na uređaju.</p> }
          <button [disabled]="busy()" (click)="confirm()">Potvrdi moje uređaje</button>
          <button [disabled]="busy()" (click)="cancel()">Otkaži</button>
        }
      </div>
      @if (auth.isProfessor()) {
        <form class="panel" (ngSubmit)="createAssistant()">
          <h3>Novi nalog asistenta</h3>
          <label>Korisničko ime <input name="username" [(ngModel)]="username" minlength="3" maxlength="80" required autocomplete="off" /></label>
          <label>Lozinka <input name="password" type="password" [(ngModel)]="password" minlength="12" required autocomplete="new-password" /></label>
          <button [disabled]="busy()">Kreiraj asistenta</button>
        </form>
      }
    </section>`,
  styles: [`.page{max-width:900px;margin:auto;padding:24px}.panel{padding:20px;margin:20px 0;border:1px solid #ccd5df;border-radius:12px}label{display:inline-block;margin:8px}input,select,button{padding:8px;margin:6px}code{overflow-wrap:anywhere}.candidate{padding:8px;border-bottom:1px solid #ddd}[role=alert]{color:#ae2424}li{margin:12px 0}`]
})
export class Staff implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  readonly rooms = CLASSROOMS;
  readonly devices = signal<StaffDevice[]>([]);
  readonly scan = signal<Scan | null>(null);
  readonly candidates = signal<Candidate[]>([]);
  readonly error = signal(''); readonly notice = signal(''); readonly busy = signal(false);
  roomId = CLASSROOMS[0].id; username = ''; password = '';
  selected: Record<string, boolean> = {}; labels: Record<string, string> = {};
  private timer?: ReturnType<typeof setInterval>;
  ngOnInit(): void {
    this.load();
    this.http.get<Scan | null>('/api/staff/scans/current').subscribe({next: scan => {
      if (scan) { this.scan.set(scan); this.timer = setInterval(() => this.refresh(), 2500); this.refresh(); }
    }, error: this.failed});
  }
  ngOnDestroy(): void { clearInterval(this.timer); }
  private failed = (e: HttpErrorResponse) => { this.busy.set(false); this.error.set(e.error?.title ?? 'Zahtev nije uspeo. Pokušajte ponovo.'); };
  private load(): void { this.http.get<StaffDevice[]>('/api/staff/devices').subscribe({next: d => this.devices.set(d), error: this.failed}); }
  start(): void {
    this.error.set(''); this.busy.set(true);
    this.http.post<Scan>('/api/staff/scans', {roomId: this.roomId}).subscribe({next: scan => {
      this.scan.set(scan); this.busy.set(false); this.selected = {}; this.labels = {}; this.candidates.set([]);
      this.timer = setInterval(() => this.refresh(), 2500); this.refresh();
    }, error: this.failed});
  }
  private refresh(): void {
    const id = this.scan()?.id; if (!id) return;
    this.http.get<{scan: Scan; candidates: Candidate[]}>(`/api/staff/scans/${id}`).subscribe({next: data => {
      if (this.scan()?.id === id) this.candidates.set(data.candidates);
    }, error: this.failed});
  }
  confirm(): void {
    const devices = this.candidates().filter(d => this.selected[d.id]).map(d => ({deviceId: d.id, label: this.labels[d.id]?.trim()}));
    if (!devices.length || devices.some(d => !d.label)) { this.error.set('Izaberite svoje uređaje i upišite naziv za svaki.'); return; }
    this.busy.set(true); this.error.set('');
    this.http.post(`/api/staff/scans/${this.scan()!.id}/confirm`, {devices}).subscribe({next: () => {
      this.finish(); this.load(); this.notice.set('Uređaji su registrovani. Možete pokrenuti praćenje.');
    }, error: this.failed});
  }
  cancel(): void { this.busy.set(true); this.http.post(`/api/staff/scans/${this.scan()!.id}/cancel`, {}).subscribe({next: () => this.finish(), error: this.failed}); }
  private finish(): void { clearInterval(this.timer); this.scan.set(null); this.busy.set(false); this.candidates.set([]); }
  remove(id: string): void { this.busy.set(true); this.error.set(''); this.http.delete(`/api/staff/devices/${id}`).subscribe({next: () => { this.busy.set(false); this.load(); }, error: this.failed}); }
  createAssistant(): void {
    this.busy.set(true); this.error.set('');
    this.http.post('/api/staff/assistants', {username: this.username, password: this.password}).subscribe({next: () => {
      this.busy.set(false); this.notice.set('Nalog asistenta je kreiran.'); this.username = ''; this.password = '';
    }, error: this.failed});
  }
}
