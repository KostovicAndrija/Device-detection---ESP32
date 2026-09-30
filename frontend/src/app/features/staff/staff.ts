import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-staff', imports: [FormsModule],
  template: `<section class="page"><header><p class="eyebrow">Korisnički nalozi</p><h2>Asistenti</h2><p>Kreirajte naloge asistenata koji mogu da registruju svoje uređaje i vode sopstvene sesije.</p></header>
    @if(error()){<p role="alert">{{error()}}</p>}@if(notice()){<p role="status">{{notice()}}</p>}
    @if(auth.isProfessor()){<form class="panel" (ngSubmit)="createAssistant()"><h3>Novi nalog asistenta</h3><label>Korisničko ime<input name="username" [(ngModel)]="username" minlength="3" maxlength="80" required autocomplete="off"/></label><label>Lozinka<input name="password" type="password" [(ngModel)]="password" minlength="12" required autocomplete="new-password"/></label><button class="add-button" [disabled]="busy()"><span class="material-symbols-outlined" aria-hidden="true">person_add</span>{{busy()?'Kreiranje…':'Kreiraj asistenta'}}</button></form>}@else{<p class="panel">Samo profesor može da kreira naloge asistenata.</p>}
  </section>`,
  styles: [`.page{margin:auto;max-width:760px;padding:24px}.page header>p{color:#64748b}.panel{background:white;border:1px solid #dce4ed;border-radius:14px;display:grid;gap:16px;margin-top:24px;padding:22px}.panel label{color:#475569;display:grid;font-size:.85rem;gap:6px}.panel input{width:100%}[role=alert]{color:#b42318}[role=status]{color:#166534}`]
})
export class Staff {
  private readonly http=inject(HttpClient); readonly auth=inject(AuthService);
  readonly busy=signal(false); readonly error=signal(''); readonly notice=signal('');
  username=''; password='';
  createAssistant():void{this.busy.set(true);this.error.set('');this.notice.set('');this.http.post('/api/staff/assistants',{username:this.username,password:this.password}).subscribe({next:()=>{this.busy.set(false);this.notice.set('Nalog asistenta je kreiran.');this.username='';this.password='';},error:(e:HttpErrorResponse)=>{this.busy.set(false);this.error.set(e.error?.title??'Kreiranje asistenta nije uspelo.');}});}
}
