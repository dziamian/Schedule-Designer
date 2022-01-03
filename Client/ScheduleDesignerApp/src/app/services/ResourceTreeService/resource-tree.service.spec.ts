import { TestBed } from '@angular/core/testing';

import { ResourceTreeService } from './resource-tree.service';

describe('ResourceTreeService', () => {
  let service: ResourceTreeService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ResourceTreeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
