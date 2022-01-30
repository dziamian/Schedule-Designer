import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CourseFieldComponent } from './course-field.component';

describe('CourseFieldComponent', () => {
  let component: CourseFieldComponent;
  let fixture: ComponentFixture<CourseFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CourseFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CourseFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
