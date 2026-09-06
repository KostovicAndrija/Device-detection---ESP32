import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { Realtime } from './realtime';

describe('Realtime', () => {
  let service: Realtime;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideRouter([])] });
    service = TestBed.inject(Realtime);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
