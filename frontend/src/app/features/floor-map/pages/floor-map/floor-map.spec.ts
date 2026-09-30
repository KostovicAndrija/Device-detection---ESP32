import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, ActivatedRoute, convertToParamMap } from '@angular/router';
import { Api } from '../../../../core/services/api';
import { of } from 'rxjs';
import { computerLabel, layoutComputers, RoomLayout, roomBoards } from '../../../../core/services/rooms';

import { FloorMap } from './floor-map';

describe('FloorMap', () => {
  let component: FloorMap;
  let fixture: ComponentFixture<FloorMap>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FloorMap],
      providers: [provideHttpClient(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
        { provide: Api, useValue: { sessions: () => of([]), positions: () => of([]) } }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FloorMap);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('toggles the enlarged map and closes it with the collapse action', () => {
    component.toggleMapSize();
    expect(component.mapExpanded()).toBeTrue();
    component.collapseMap();
    expect(component.mapExpanded()).toBeFalse();
  });

  it('shows a default board on the top wall when a room has none', () => {
    component.savedLayout.set({id:'NEW',name:'Nova',description:'',width:10,height:7,revision:1,computers:[],sensors:[]});
    const board=roomBoards(component.selectedClassroom())[0];
    expect(board.x).toBe(5);
    expect(board.y).toBe(board.height/2);
  });

  it('uses saved room dimensions and computer coordinates', () => {
    component.savedLayout.set({id:'NEW',name:'Nova',description:'',width:20,height:10,revision:1,
      computers:[{id:'PC-7',x:10,y:2}],sensors:[]});
    expect(component.left(10)).toBe('50%');
    expect(component.top(2)).toBe('20%');
    expect(component.desks()[0]).toEqual(jasmine.objectContaining({id:'PC-7',left:50,top:20}));
  });

  it('distributes computers with equal inner and outer gaps', () => {
    const slots=layoutComputers({id:'K-1',x:1,y:1,width:2,height:.6,kind:'desk',computerCount:2},2);
    const leftGap=slots[0].offset-slots[0].width/2+1;
    const innerGap=slots[1].offset-slots[1].width/2-(slots[0].offset+slots[0].width/2);
    const rightGap=1-(slots[1].offset+slots[1].width/2);
    expect(slots.length).toBe(2);
    expect(leftGap).toBeCloseTo(innerGap,10);
    expect(rightGap).toBeCloseTo(innerGap,10);

    const crowded=layoutComputers({id:'K-2',x:1,y:1,width:1,height:.6,kind:'desk',computerCount:8},1);
    expect(crowded.length).toBe(8);
    expect(crowded[0].width).toBeLessThan(slots[0].width);
  });

  it('labels every classroom computer sequentially as plab', () => {
    const room:RoomLayout={id:'LAB',name:'Lab',description:'',width:8,height:6,revision:1,sensors:[],computers:[
      {id:'K-1',x:2,y:2,width:2,height:.6,kind:'desk',computerCount:2},
      {id:'K-2',x:5,y:2,width:2,height:.6,kind:'desk',computerCount:3}
    ]};
    expect(computerLabel(room,0,0)).toBe('plab1');
    expect(computerLabel(room,0,1)).toBe('plab2');
    expect(computerLabel(room,1,0)).toBe('plab3');
    expect(computerLabel(room,1,2)).toBe('plab5');
  });

  it('shows radio type and uses replay time rather than wall clock for historical markers', () => {
    const at=Date.parse('2026-01-01T12:00:00Z');
    component.replayMode.set(true);component.replayAt.set(at);
    component.positions.set([
      {deviceId:'wifi-id',x:1,y:1,confidence:.8,sensorCount:3,isWhitelisted:false,signalType:'wifi',capturedAt:new Date(at).toISOString()},
      {deviceId:'ble-id',x:2,y:2,confidence:.8,sensorCount:3,isWhitelisted:true,signalType:'bluetooth',capturedAt:new Date(at).toISOString()}
    ]);
    fixture.detectChanges();
    expect(component.activeCount()).toBe(2);
    const labels=Array.from(fixture.nativeElement.querySelectorAll('.device-icon')).map((node:any)=>node.textContent.trim());
    expect(labels).toEqual(['wifi','bluetooth']);
    component.replayAt.set(at+46000);
    expect(component.visiblePositions()).toEqual([]);
  });

  it('should fade and hide devices as their readings age', () => {
    const now = Date.now();
    component.clock.set(now);
    component.positions.set([
      { deviceId: 'live', x: 1, y: 1, confidence: .9, sensorCount: 3, isWhitelisted: false, capturedAt: new Date(now - 2_000).toISOString() },
      { deviceId: 'fading', x: 2, y: 2, confidence: .8, sensorCount: 3, isWhitelisted: false, capturedAt: new Date(now - 9_000).toISOString() },
      { deviceId: 'hidden', x: 3, y: 3, confidence: .7, sensorCount: 3, isWhitelisted: false, capturedAt: new Date(now - 46_000).toISOString() }
    ]);

    expect(component.activeCount()).toBe(1);
    expect(component.fadingCount()).toBe(1);
    expect(component.visiblePositions().map(position => position.deviceId)).toEqual(['live', 'fading']);
    expect(component.opacity(component.positions()[1])).toBeLessThan(1);
  });
});
