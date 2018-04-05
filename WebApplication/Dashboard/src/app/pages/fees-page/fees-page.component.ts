import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {APIService, MessageBoxService} from "../../services";

@Component({
  selector: 'app-fees-page',
  templateUrl: './fees-page.component.html',
  styleUrls: ['./fees-page.component.sass']
})
export class FeesPageComponent implements OnInit {

  public loading = false;
  public isDataLoaded = false;
  public currencyTypeList = ['fiat', 'crypto']
  public currentCurrencyType = this.currencyTypeList[0];
  public fees: object;

  constructor(private _cdRef: ChangeDetectorRef,
              private apiService: APIService,
              private _messageBox: MessageBoxService) { }

  ngOnInit() {
    this.apiService.getFees().subscribe(data => {
      this.fees = data.data;
      this.isDataLoaded = true;
      this._cdRef.detectChanges();
    });
  }

  chooseCurrencyType(type) {
    if (this.currentCurrencyType !== type) {
      this.currentCurrencyType = type;
      this._cdRef.detectChanges();
    }
  }

  updateFees() {
    this.loading = true;
    this.apiService.updateFees(this.fees).subscribe(() => {
      this._messageBox.alert('Changes saved');
      this.loading = false;
      this._cdRef.detectChanges();
    });
  }

}
