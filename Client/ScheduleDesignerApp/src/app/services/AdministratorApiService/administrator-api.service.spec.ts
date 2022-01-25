import { TestBed } from '@angular/core/testing';

import { AdministratorApiService } from './administrator-api.service';

describe('AdministratorApiService', () => {
  let service: AdministratorApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AdministratorApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
