import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GlActivityComponent } from './gl-activity-component';

describe('GlActivityComponent', () => {
  let component: GlActivityComponent;
  let fixture: ComponentFixture<GlActivityComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlActivityComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GlActivityComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
