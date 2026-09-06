import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { Api } from './api';

describe('Api', () => {
  let service: Api;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    service = TestBed.inject(Api);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
