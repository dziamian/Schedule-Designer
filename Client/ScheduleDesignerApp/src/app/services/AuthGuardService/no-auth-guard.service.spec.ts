import { TestBed } from '@angular/core/testing';

import { NoAuthGuardService } from './no-auth-guard.service';

describe('NoAuthGuardService', () => {
  let service: NoAuthGuardService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NoAuthGuardService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
