import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, Input, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DevicePosition } from '../../core/services/api';
import { RoomLayout } from '../../core/services/rooms';
import { RoomMapComponent } from '../../shared/room-map/room-map';

interface StaffDevice { id: string; label: string; deviceHash: string; }
interface Scan { id: string; roomId: string; status: string; registrationExpiresAt: string; }
interface Candidate { id: string; hashId: string; type: string; rssi: number; }

@Component({
  selector: 'app-device-registration', imports: [FormsModule, DatePipe, RoomMapComponent],
  template: `<section class="registration-panel">
    <header><div><p class="eyebrow">Dozvoljeni lični uređaji</p><h3>Moji uređaji</h3></div>@if(!scan()){<button class="scan-button" [disabled]="busy()||monitoringActive" (click)="start()"><span class="material-symbols-outlined" aria-hidden="true">radar</span>Skeniraj moje uređaje</button>}</header>
    <p>Registrovani uređaji se automatski dozvoljavaju u svakoj narednoj sesiji koju pokrenete.</p>
    @if(monitoringActive&&!scan()){<p class="hint">Zaustavite aktivno očitavanje pre registracije novog uređaja.</p>}@if(error()){<p role="alert">{{error()}}</p>}@if(notice()){<p role="status">{{notice()}}</p>}
    <ul class="saved-devices">@for(device of devices();track device.id){<li><span><strong>{{device.label}}</strong><small>{{device.deviceHash.slice(0,16)}}…</small></span><button class="quiet" [disabled]="busy()||!!scan()||monitoringActive" (click)="remove(device.id)"><span class="material-symbols-outlined" aria-hidden="true">delete</span>Ukloni</button></li>}@empty{<li class="empty">Još nemate registrovane uređaje.</li>}</ul>
    @if(scan()){<div class="scan-workspace"><p>Prikupljanje očitavanja do {{scan()!.registrationExpiresAt|date:'mediumTime'}}. Nakon toga imate deset minuta da potvrdite izbor.</p><p>Senzori moraju slati očitavanja za ovu registraciju:</p><code>{{scan()!.id}}</code><p>Simulator:</p><code>docker compose --profile tools run --rm --build simulator registration --session {{scan()!.id}} --x 2.5 --y 4</code>
      @if(layout();as room){<div class="registration-map-wrap"><div class="map-heading"><strong>{{room.name}}</strong><small>{{positions().length}} pozicioniranih uređaja</small></div><app-room-map [room]="room" [positions]="positions()" [compact]="true"/>@if(!positions().length){<p class="map-empty">Čekamo da senzori odrede položaj uređaja.</p>}</div>}
      <div class="candidates">@for(candidate of candidates();track candidate.id){<div class="candidate"><label><input type="checkbox" [(ngModel)]="selected[candidate.id]"/>{{shortId(candidate.id)}} · {{candidate.type}} · {{candidate.hashId.slice(0,16)}} · {{candidate.rssi}} dBm</label>@if(selected[candidate.id]){<input aria-label="Naziv uređaja" placeholder="Npr. moj telefon" maxlength="100" [(ngModel)]="labels[candidate.id]"/>}</div>}@empty{<p>Čekamo očitavanja senzora. Uključite bežičnu komunikaciju na uređaju.</p>}</div>
      <div class="actions"><button [disabled]="busy()" (click)="confirm()"><span class="material-symbols-outlined" aria-hidden="true">check</span>Potvrdi moje uređaje</button><button class="quiet" [disabled]="busy()" (click)="cancel()"><span class="material-symbols-outlined" aria-hidden="true">close</span>Otkaži</button></div></div>}
  </section>`,
  styles: [`:host{display:block}.registration-panel{background:white;border:1px solid #dce4ed;border-radius:16px;box-shadow:0 12px 32px #0f172a0d;padding:20px}.registration-panel>header{align-items:center;display:flex;gap:16px;justify-content:space-between}.registration-panel h3{margin:.2rem 0}.registration-panel p{color:#64748b}.scan-button{flex:none}.hint{background:#fff7ed;border:1px solid #fed7aa;border-radius:8px;color:#9a3412!important;padding:10px 12px}[role=alert]{color:#b42318!important}[role=status]{color:#166534!important}.saved-devices{display:grid;gap:8px;list-style:none;margin:16px 0 0;padding:0}.saved-devices li{align-items:center;background:#f8fafc;border:1px solid #e2e8f0;border-radius:9px;display:flex;justify-content:space-between;padding:9px 12px}.saved-devices li>span{display:flex;flex-direction:column;gap:3px}.saved-devices small{color:#64748b}.saved-devices .empty{color:#64748b;display:block}.quiet{background:#f1f5f9;border:1px solid #dbe3ed;color:#334155}.scan-workspace{border-top:1px solid #e2e8f0;margin-top:18px;padding-top:18px}.scan-workspace code{background:#0f172a;border-radius:6px;color:#e2e8f0;display:block;overflow-wrap:anywhere;padding:.65rem}.registration-map-wrap{background:#f8fafc;border:1px solid #cbd5e1;border-radius:12px;margin:16px 0;padding:14px}.map-heading{display:flex;justify-content:space-between;margin-bottom:8px}.map-heading small,.map-empty{color:#64748b}.map-empty{text-align:center}.candidate{align-items:center;border-bottom:1px solid #e2e8f0;display:flex;flex-wrap:wrap;gap:10px;padding:9px 0}.candidate label{align-items:center;display:flex;gap:7px}.candidate>input{flex:1;min-width:180px}.actions{display:flex;gap:10px;margin-top:16px}@media(max-width:650px){.registration-panel>header,.map-heading{align-items:flex-start;flex-direction:column}.scan-button{width:100%}.saved-devices li{align-items:flex-start;flex-direction:column}.actions{flex-direction:column}}`]
})
export class DeviceRegistrationComponent implements OnInit, OnDestroy {
  private readonly http=inject(HttpClient);
  @Input({required:true}) roomId=''; @Input() monitoringActive=false;
  readonly devices=signal<StaffDevice[]>([]); readonly scan=signal<Scan|null>(null); readonly candidates=signal<Candidate[]>([]); readonly layout=signal<RoomLayout|null>(null); readonly positions=signal<DevicePosition[]>([]);
  readonly error=signal(''); readonly notice=signal(''); readonly busy=signal(false);
  selected:Record<string,boolean>={}; labels:Record<string,string>={}; private timer?:ReturnType<typeof setInterval>;
  ngOnInit():void{this.loadDevices();this.http.get<Scan|null>('/api/staff/scans/current').subscribe({next:scan=>{if(scan)this.resume(scan);},error:this.failed});}
  ngOnDestroy():void{clearInterval(this.timer);}
  start():void{if(this.monitoringActive||!this.roomId)return;this.error.set('');this.notice.set('');this.busy.set(true);this.http.post<Scan>('/api/staff/scans',{roomId:this.roomId}).subscribe({next:scan=>{this.busy.set(false);this.selected={};this.labels={};this.candidates.set([]);this.resume(scan);},error:this.failed});}
  confirm():void{const devices=this.candidates().filter(device=>this.selected[device.id]).map(device=>({deviceId:device.id,label:this.labels[device.id]?.trim()}));if(!devices.length||devices.some(device=>!device.label)){this.error.set('Izaberite svoje uređaje i upišite naziv za svaki.');return;}this.busy.set(true);this.error.set('');this.http.post(`/api/staff/scans/${this.scan()!.id}/confirm`,{devices}).subscribe({next:()=>{this.finish();this.loadDevices();this.notice.set('Uređaji su registrovani. Možete pokrenuti praćenje.');},error:this.failed});}
  cancel():void{this.busy.set(true);this.http.post(`/api/staff/scans/${this.scan()!.id}/cancel`,{}).subscribe({next:()=>this.finish(),error:this.failed});}
  remove(id:string):void{if(this.monitoringActive)return;this.busy.set(true);this.error.set('');this.http.delete(`/api/staff/devices/${id}`).subscribe({next:()=>{this.busy.set(false);this.loadDevices();},error:this.failed});}
  shortId(id:string):string{return id.slice(0,6).toUpperCase();}
  private loadDevices():void{this.http.get<StaffDevice[]>('/api/staff/devices').subscribe({next:devices=>this.devices.set(devices),error:this.failed});}
  private refresh():void{const id=this.scan()?.id;if(!id)return;this.http.get<{scan:Scan;candidates:Candidate[]}>(`/api/staff/scans/${id}`).subscribe({next:data=>{if(this.scan()?.id===id)this.candidates.set(data.candidates);},error:this.failed});this.http.get<DevicePosition[]>(`/api/sessions/${id}/positions`).subscribe({next:positions=>{if(this.scan()?.id===id)this.positions.set(positions);},error:this.failed});}
  private resume(scan:Scan):void{this.scan.set(scan);this.positions.set([]);this.http.get<{layout:RoomLayout}>(`/api/sessions/${scan.id}/layout`).subscribe({next:data=>this.layout.set(data.layout),error:this.failed});clearInterval(this.timer);this.timer=setInterval(()=>this.refresh(),2500);this.refresh();}
  private finish():void{clearInterval(this.timer);this.scan.set(null);this.busy.set(false);this.candidates.set([]);this.layout.set(null);this.positions.set([]);}
  private failed=(error:HttpErrorResponse)=>{this.busy.set(false);this.error.set(error.error?.title??'Zahtev nije uspeo. Pokušajte ponovo.');};
}
