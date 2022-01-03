import { TestBed } from '@angular/core/testing';

import { ScheduleInteractionService } from './schedule-interaction.service';

describe('ScheduleInteractionService', () => {
  let service: ScheduleInteractionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ScheduleInteractionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
