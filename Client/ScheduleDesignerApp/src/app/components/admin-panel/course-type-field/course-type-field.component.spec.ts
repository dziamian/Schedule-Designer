import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CourseTypeFieldComponent } from './course-type-field.component';

describe('CourseTypeFieldComponent', () => {
  let component: CourseTypeFieldComponent;
  let fixture: ComponentFixture<CourseTypeFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CourseTypeFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CourseTypeFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
