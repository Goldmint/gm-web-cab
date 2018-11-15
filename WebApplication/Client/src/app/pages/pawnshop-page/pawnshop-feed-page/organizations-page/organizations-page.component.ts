import {
  ChangeDetectionStrategy, ChangeDetectorRef,
  Component,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {UserService} from "../../../../services";
import {Subject} from "rxjs/Subject";

@Component({
  selector: 'app-organizations-page',
  templateUrl: './organizations-page.component.html',
  styleUrls: ['./organizations-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrganizationsPageComponent implements OnInit {

  public currentStep: number = 1;
  public currentId: number = null;
  public stepperData;
  public isDenied: boolean = true;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.userService.organizationStepper$.takeUntil(this.destroy$).subscribe((data: any) => {
      if (data !== null) {
        this.stepperData = data;
        this.currentId = data.id;
        this.checkStepAccess();
      }
    });
  }

  checkStepAccess() {
    this.isDenied = true;
    this.isDenied = this.currentStep === 1 && !this.stepperData.org;
    this.isDenied = this.currentStep === 2 && !this.stepperData.pawnshop;
    this.cdRef.markForCheck();
  }

  prevStep() {
    this.currentStep--;
    this.userService.organizationStepper$.next(this.stepperData);
    this.cdRef.markForCheck();
  }

  nextStep() {
    this.currentStep++;
    let data = this.stepperData;
    data.step = this.currentStep;
    data.id = this.currentId;
    this.userService.organizationStepper$.next(data);
    this.cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.userService.organizationStepper$.next(null);
    this.destroy$.next(true);
  }
}
