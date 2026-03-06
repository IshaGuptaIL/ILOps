import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvoiceCreditComponent } from './invoice-credit-component';

describe('InvoiceCreditComponent', () => {
  let component: InvoiceCreditComponent;
  let fixture: ComponentFixture<InvoiceCreditComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceCreditComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InvoiceCreditComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
