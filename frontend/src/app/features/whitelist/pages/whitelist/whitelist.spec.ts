import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Whitelist } from './whitelist';

describe('Whitelist', () => {
  let component: Whitelist;
  let fixture: ComponentFixture<Whitelist>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Whitelist]
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
