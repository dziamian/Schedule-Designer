import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClearFieldComponent } from './clear-field.component';

describe('ClearFieldComponent', () => {
  let component: ClearFieldComponent;
  let fixture: ComponentFixture<ClearFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClearFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClearFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
