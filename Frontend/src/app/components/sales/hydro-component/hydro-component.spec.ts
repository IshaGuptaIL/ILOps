import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HydroComponent } from './hydro-component';

describe('HydroComponent', () => {
  let component: HydroComponent;
  let fixture: ComponentFixture<HydroComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HydroComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HydroComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
