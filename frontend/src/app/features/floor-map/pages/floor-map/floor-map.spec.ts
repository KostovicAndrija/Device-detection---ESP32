import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { FloorMap } from './floor-map';

describe('FloorMap', () => {
  let component: FloorMap;
  let fixture: ComponentFixture<FloorMap>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FloorMap],
      providers: [provideHttpClient()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FloorMap);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
