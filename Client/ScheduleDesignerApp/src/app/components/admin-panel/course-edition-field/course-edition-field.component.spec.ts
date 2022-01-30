import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CourseEditionFieldComponent } from './course-edition-field.component';

describe('CourseEditionFieldComponent', () => {
  let component: CourseEditionFieldComponent;
  let fixture: ComponentFixture<CourseEditionFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CourseEditionFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CourseEditionFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
