import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PersonalScheduleComponent } from './personal-schedule.component';

describe('PersonalScheduleComponent', () => {
  let component: PersonalScheduleComponent;
  let fixture: ComponentFixture<PersonalScheduleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ PersonalScheduleComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PersonalScheduleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
