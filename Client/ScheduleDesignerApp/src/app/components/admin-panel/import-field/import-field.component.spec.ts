import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ImportFieldComponent } from './import-field.component';

describe('ImportFieldComponent', () => {
  let component: ImportFieldComponent;
  let fixture: ComponentFixture<ImportFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ImportFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ImportFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
