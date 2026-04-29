import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IPOInvestment } from './ipo-investment';

describe('IPOInvestment', () => {
  let component: IPOInvestment;
  let fixture: ComponentFixture<IPOInvestment>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IPOInvestment],
    }).compileComponents();

    fixture = TestBed.createComponent(IPOInvestment);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
