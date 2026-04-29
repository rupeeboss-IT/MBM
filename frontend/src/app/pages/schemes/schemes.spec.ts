import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Schemes } from './schemes';

describe('Schemes', () => {
  let component: Schemes;
  let fixture: ComponentFixture<Schemes>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Schemes],
    }).compileComponents();

    fixture = TestBed.createComponent(Schemes);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
