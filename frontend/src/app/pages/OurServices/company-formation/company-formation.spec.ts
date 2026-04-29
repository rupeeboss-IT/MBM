import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CompanyFormation } from './company-formation';

describe('CompanyFormation', () => {
  let component: CompanyFormation;
  let fixture: ComponentFixture<CompanyFormation>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompanyFormation],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyFormation);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
