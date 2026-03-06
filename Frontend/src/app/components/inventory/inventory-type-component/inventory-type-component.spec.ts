import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryTypeComponent } from './inventory-type-component';

describe('InventoryTypeComponent', () => {
  let component: InventoryTypeComponent;
  let fixture: ComponentFixture<InventoryTypeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryTypeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InventoryTypeComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
