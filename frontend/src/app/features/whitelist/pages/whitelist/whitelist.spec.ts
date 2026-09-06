import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { Whitelist } from './whitelist';

describe('Whitelist', () => {
  let component: Whitelist;
  let fixture: ComponentFixture<Whitelist>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Whitelist],
      providers: [provideHttpClient()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Whitelist);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
