import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PriceProtectionComponent } from './price-protection-component';

describe('PriceProtectionComponent', () => {
  let component: PriceProtectionComponent;
  let fixture: ComponentFixture<PriceProtectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PriceProtectionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PriceProtectionComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
