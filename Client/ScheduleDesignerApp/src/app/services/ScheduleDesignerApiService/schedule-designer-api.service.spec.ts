import { TestBed } from '@angular/core/testing';

import { ScheduleDesignerApiService } from './schedule-designer-api.service';

describe('ScheduleDesignerApiService', () => {
  let service: ScheduleDesignerApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ScheduleDesignerApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
