import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, ActivatedRoute, convertToParamMap } from '@angular/router';
import { Api } from '../../../../core/services/api';
import { of } from 'rxjs';

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
