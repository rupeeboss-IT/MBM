import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GovtSubsidies } from './govt-subsidies';

describe('GovtSubsidies', () => {
  let component: GovtSubsidies;
  let fixture: ComponentFixture<GovtSubsidies>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GovtSubsidies],
    }).compileComponents();

    fixture = TestBed.createComponent(GovtSubsidies);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
