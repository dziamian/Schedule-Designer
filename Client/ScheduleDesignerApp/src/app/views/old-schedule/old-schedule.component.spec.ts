import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OldScheduleComponent } from './old-schedule.component';

describe('OldScheduleComponent', () => {
  let component: OldScheduleComponent;
  let fixture: ComponentFixture<OldScheduleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ OldScheduleComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(OldScheduleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
