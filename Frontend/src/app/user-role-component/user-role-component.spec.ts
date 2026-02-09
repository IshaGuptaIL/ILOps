import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserRoleComponent } from './user-role-component';

describe('UserRoleComponent', () => {
  let component: UserRoleComponent;
  let fixture: ComponentFixture<UserRoleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserRoleComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserRoleComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
