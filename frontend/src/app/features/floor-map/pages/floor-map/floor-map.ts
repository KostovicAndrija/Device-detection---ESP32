import { DatePipe, DecimalPipe, UpperCasePipe } from '@angular/common';
import { Component, HostListener, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY_ROOM, RoomLayout, Rooms } from '../../../../core/services/rooms';
import { Api, DevicePosition, ExamSession } from '../../../../core/services/api';
import { RoomMapComponent } from '../../../../shared/room-map/room-map';
import { DeviceRegistrationComponent } from '../../../device-registration/device-registration';

@Component({selector:'app-floor-map',imports:[FormsModule,DatePipe,DecimalPipe,UpperCasePipe,RoomMapComponent,DeviceRegistrationComponent],templateUrl:'./floor-map.html',styleUrl:'./floor-map.scss'})
export class FloorMap implements OnInit, OnDestroy {
  private readonly api=inject(Api); private readonly route=inject(ActivatedRoute); private readonly router=inject(Router);
  readonly roomStore=inject(Rooms); readonly classrooms=this.roomStore.items;
  readonly sessions=signal<ExamSession[]>([]); readonly positions=signal<DevicePosition[]>([]);
  readonly clock=signal(Date.now()); readonly loading=signal(false); readonly error=signal(''); readonly lastRefresh=signal<Date|null>(null);
  private readonly roomKey=signal('UC-101'); private readonly sessionKey=signal('');
  get roomId(){return this.roomKey();} set roomId(value:string){this.roomKey.set(value);}
  get sessionId(){return this.sessionKey();} set sessionId(value:string){this.sessionKey.set(value);}
  readonly savedLayout=signal<RoomLayout|null>(null); readonly hasSnapshot=signal(true);
  readonly selectedSession=computed(()=>this.sessions().find(s=>s.id===this.sessionKey()));
  readonly selectedClassroom=computed(()=>this.savedLayout()??this.classrooms().find(r=>r.id===this.roomKey())??EMPTY_ROOM);
  readonly desks=computed(()=>this.selectedClassroom().computers.map(p=>({id:p.id,left:p.x/this.selectedClassroom().width*100,top:p.y/this.selectedClassroom().height*100,width:p.width?p.width/this.selectedClassroom().width*100:null,height:p.height?p.height/this.selectedClassroom().height*100:null,isDesk:p.kind==='desk'})));
  readonly replayMode=signal(false); readonly playing=signal(false); readonly replayAt=signal(0);
  readonly mapExpanded=signal(false);
  readonly firstAt=signal(0); readonly lastAt=signal(0); readonly observationCount=signal(0);
  readonly viewTime=computed(()=>this.replayMode()?this.replayAt():this.clock());
  readonly visiblePositions=computed(()=>this.positions().filter(p=>this.ageMs(p,this.viewTime())<45000));
  readonly activeCount=computed(()=>this.positions().filter(p=>this.ageMs(p,this.viewTime())<8000).length);
  readonly fadingCount=computed(()=>this.visiblePositions().length-this.activeCount());
  speed=1; private timer?:ReturnType<typeof setInterval>; private poll=0; private request=0;
  ngOnInit():void {
    this.roomStore.load();
    this.roomId=this.route.snapshot.queryParamMap.get('room')??'UC-101';
    const requested=this.route.snapshot.queryParamMap.get('session');
    this.api.sessions().subscribe({next:sessions=>{this.sessions.set(sessions);this.sessionId=sessions.find(s=>s.id===requested)?.id??sessions.find(s=>s.roomId===this.roomId&&s.status==='active')?.id??'';this.selectSession();},error:()=>this.error.set('Sesije nije moguće učitati.')});
    this.timer=setInterval(()=>{
      this.clock.set(Date.now());
      if(this.replayMode()) {
        if(this.playing()&&!this.loading()) { this.replayAt.set(Math.min(this.lastAt(),this.replayAt()+1000*this.speed));this.load(false);if(this.replayAt()>=this.lastAt())this.playing.set(false); }
      } else if(++this.poll%2===0) this.load(false);
    },1000);
  }
  ngOnDestroy():void{clearInterval(this.timer);this.request++;}
  @HostListener('document:keydown.escape')
  collapseMap():void{this.mapExpanded.set(false);}
  toggleMapSize():void{this.mapExpanded.update(value=>!value);}
  load(showSpinner=true):void{
    if(!this.sessionId){this.loading.set(false);return;}
    const id=this.sessionId;const serial=++this.request;
    if(showSpinner||this.replayMode())this.loading.set(true);
    const result=this.replayMode()?this.api.replay(id,new Date(this.replayAt()).toISOString()):this.api.positions(id);
    result.subscribe({next:positions=>{if(serial!==this.request||id!==this.sessionId)return;this.positions.set(positions);this.lastRefresh.set(new Date());this.loading.set(false);},error:e=>{if(serial!==this.request)return;this.error.set(e.error?.title??'Pozicije nije moguće učitati.');this.loading.set(false);this.playing.set(false);}});
  }
  selectSession():void{
    this.request++;this.playing.set(false);this.replayMode.set(false);this.positions.set([]);this.savedLayout.set(null);this.error.set('');this.observationCount.set(0);this.loading.set(false);
    const session=this.selectedSession();if(session)this.roomId=session.roomId;
    this.updateUrl();if(!this.sessionId)return;
    const id=this.sessionId;
    this.api.sessionLayout(id).subscribe({next:data=>{if(this.sessionId===id){this.savedLayout.set(data.layout);this.hasSnapshot.set(data.hasSnapshot);}},error:()=>this.error.set('Raspored sesije nije moguće učitati.')});
    this.api.history(id).subscribe({next:h=>{if(this.sessionId!==id)return;this.observationCount.set(h.observationCount);this.firstAt.set(h.firstAt?Date.parse(h.firstAt):0);this.lastAt.set(h.lastAt?Date.parse(h.lastAt):0);
      if(session?.status==='completed'&&h.firstAt){this.replayMode.set(true);this.replayAt.set(this.firstAt());}this.load();},error:()=>this.error.set('Istoriju nije moguće učitati.')});
  }
  selectRoom():void{this.sessionId=this.sessions().find(s=>s.roomId===this.roomId&&s.status==='active')?.id??'';this.selectSession();}
  toggleReplay():void{
    if(this.replayMode()){this.playing.set(false);this.replayMode.set(false);this.load();return;}
    const id=this.sessionId;
    this.api.history(id).subscribe({next:h=>{if(id!==this.sessionId)return;this.observationCount.set(h.observationCount);if(!h.firstAt||!h.lastAt){this.error.set('Nema sačuvanih očitavanja za reprodukciju.');return;}this.firstAt.set(Date.parse(h.firstAt));this.lastAt.set(Date.parse(h.lastAt));this.replayAt.set(this.firstAt());this.replayMode.set(true);this.load();},error:()=>this.error.set('Istoriju nije moguće učitati.')});
  }
  seek(value:number):void{this.playing.set(false);this.replayAt.set(Number(value));this.load();}
  play():void{if(this.replayAt()>=this.lastAt())this.replayAt.set(this.firstAt());this.playing.update(v=>!v);}
  startReading():void{this.loading.set(true);this.error.set('');this.api.startRoomSession(this.roomId).subscribe({next:s=>{this.sessions.update(items=>[s,...items]);this.sessionId=s.id;this.selectSession();},error:e=>{this.loading.set(false);this.error.set(e.error?.title??'Praćenje nije moguće pokrenuti.');}});}
  stopReading():void{const s=this.selectedSession();if(!s||s.status!=='active')return;this.api.stopSession(s.id).subscribe({next:result=>{if(result.deleted){this.sessions.update(items=>items.filter(x=>x.id!==s.id));this.sessionId='';}else if(result.session){this.sessions.update(items=>items.map(x=>x.id===result.session!.id?result.session!:x));}this.selectSession();},error:e=>this.error.set(e.error?.title??'Zaustavljanje nije uspelo.')});}
  left(x:number):string{return `${Math.max(0,Math.min(100,x/this.selectedClassroom().width*100))}%`;}
  top(y:number):string{return `${Math.max(0,Math.min(100,y/this.selectedClassroom().height*100))}%`;}
  opacity(p:DevicePosition):number{const age=this.ageMs(p,this.viewTime());return age<=8000?1:Math.max(0,1-(age-8000)/37000);}
  presence(p:DevicePosition):'live'|'fading'{return this.ageMs(p,this.viewTime())<8000?'live':'fading';}
  private ageMs(p:DevicePosition,now:number):number{return Math.max(0,now-Date.parse(p.capturedAt));}
  private updateUrl():void{void this.router.navigate([],{relativeTo:this.route,queryParams:{room:this.roomId,session:this.sessionId||null},replaceUrl:true});}
}
