import { Component, ElementRef, ViewChild, effect, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ComputerLocation, EMPTY_ROOM, RoomLayout, Rooms, SensorLocation } from '../../core/services/rooms';
import { RoomMapComponent } from '../../shared/room-map/room-map';

@Component({selector:'app-classrooms', imports:[FormsModule, DecimalPipe, RoomMapComponent], templateUrl:'./classrooms.html', styleUrl:'./classrooms.scss'})
export class Classrooms {
  readonly rooms=inject(Rooms); readonly auth=inject(AuthService); private readonly route=inject(ActivatedRoute);
  readonly busy=signal(false); readonly error=signal(''); readonly message=signal('');
  @ViewChild('editor') editor!:ElementRef<HTMLDialogElement>;
  draft:RoomLayout|null=null;
  mode:'room'|'desk'|'sensor'='room'; itemIndex=-1; creatingRoom=false;
  roomForm:RoomLayout=structuredClone(EMPTY_ROOM);
  item:SensorLocation={id:'',x:0,y:0,width:1.2,height:.6,kind:'desk',computerCount:0,isTeacherDesk:false,model:'ESP32',referenceRssi:-45,pathLossExponent:2.7};
  constructor(){
    effect(()=>{const rooms=this.rooms.items();if(!this.draft&&rooms.length)this.edit(rooms.find(r=>r.id===this.route.snapshot.queryParamMap.get('room'))??rooms[0]);});
    this.rooms.load();
  }
  edit(room:RoomLayout):void { this.draft=structuredClone(room); this.error.set('');this.message.set(''); }
  openRoom(create=false):void {
    this.creatingRoom=create;this.mode='room';this.roomForm=structuredClone(create?EMPTY_ROOM:this.draft!);
    if(create)this.roomForm.name='';this.open();
  }
  openItem(mode:'desk'|'sensor',index=-1):void {
    if(!this.draft||!this.auth.isProfessor())return;
    this.mode=mode;this.itemIndex=index;
    const room=this.draft;const list=mode==='desk'?room.computers:room.sensors;
    const prefix=mode==='desk'?'K-':'S';let n=1;while(list.some(p=>p.id.toLowerCase()===(prefix+n).toLowerCase()))n++;
    this.item={id:prefix+n,x:room.width/2,y:room.height/2,width:Math.min(1.2,room.width),height:Math.min(.6,room.height),kind:'desk',computerCount:0,isTeacherDesk:false,model:'ESP32',referenceRssi:-45,pathLossExponent:2.7};
    if(index>=0){Object.assign(this.item,structuredClone(list[index]));if(mode==='desk'){
      const pc=room.computers[index];this.item.kind=pc.kind??'computer';this.item.width=this.deskWidth(pc);this.item.height=this.deskHeight(pc);
    }}
    this.open();
  }
  private open():void { this.error.set('');this.message.set('');this.editor.nativeElement.showModal(); }
  close():void { if(!this.busy())this.editor.nativeElement.close(); }
  cancel(event:Event):void { if(this.busy())event.preventDefault();else this.error.set(''); }
  submit():void {
    if(this.mode==='room'){this.persist(structuredClone(this.roomForm),!this.creatingRoom);return;}
    if(!this.draft)return;
    const next=structuredClone(this.draft);const item=this.item;
    if(this.mode==='desk'){
      const pc:ComputerLocation={id:item.id.trim(),x:item.x,y:item.y,width:item.width,height:item.height,kind:item.kind,computerCount:item.computerCount,isTeacherDesk:item.isTeacherDesk};
      if(this.itemIndex<0)next.computers.push(pc);else next.computers[this.itemIndex]=pc;
    }else{
      const sensor:SensorLocation={id:item.id.trim(),x:item.x,y:item.y,model:item.model,referenceRssi:item.referenceRssi,pathLossExponent:item.pathLossExponent};
      if(this.itemIndex<0)next.sensors.push(sensor);else next.sensors[this.itemIndex]=sensor;
    }
    this.persist(next,true);
  }
  remove():void {
    if(!this.draft||this.itemIndex<0)return;
    const next=structuredClone(this.draft);
    (this.mode==='desk'?next.computers:next.sensors).splice(this.itemIndex,1);this.persist(next,true);
  }
  private persist(room:RoomLayout,existing:boolean):void {
    if(!this.auth.isProfessor()||this.busy())return;
    this.busy.set(true);this.error.set('');
    this.rooms.save(room,existing).subscribe({next:saved=>{this.edit(saved);this.busy.set(false);this.editor.nativeElement.close();this.rooms.load();this.message.set('Raspored je sačuvan.');},error:e=>{this.error.set(e.error?.title??'Čuvanje nije uspelo. Proverite unete vrednosti.');this.busy.set(false);}});
  }
  deskWidth(pc:ComputerLocation):number{return pc.width??this.draft!.width*.13;}
  deskHeight(pc:ComputerLocation):number{return pc.height??this.draft!.height*.08;}
}
