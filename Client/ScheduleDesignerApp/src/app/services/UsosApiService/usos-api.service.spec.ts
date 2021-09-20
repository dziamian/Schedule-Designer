import { TestBed } from '@angular/core/testing';

import { UsosApiService } from './usos-api.service';

describe('UsosApiService', () => {
  let service: UsosApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(UsosApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
