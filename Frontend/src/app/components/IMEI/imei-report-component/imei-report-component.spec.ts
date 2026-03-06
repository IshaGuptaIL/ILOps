import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ImeiReportComponent } from './imei-report-component';

describe('ImeiReportComponent', () => {
  let component: ImeiReportComponent;
  let fixture: ComponentFixture<ImeiReportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImeiReportComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ImeiReportComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
