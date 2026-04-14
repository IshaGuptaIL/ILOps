import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpareLightComponent } from './spare-light-component';

describe('SpareLightComponent', () => {
  let component: SpareLightComponent;
  let fixture: ComponentFixture<SpareLightComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpareLightComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SpareLightComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
