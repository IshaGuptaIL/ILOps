import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OutputInvoiceComponent } from './output-invoice-component';

describe('OutputInvoiceComponent', () => {
  let component: OutputInvoiceComponent;
  let fixture: ComponentFixture<OutputInvoiceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OutputInvoiceComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OutputInvoiceComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
