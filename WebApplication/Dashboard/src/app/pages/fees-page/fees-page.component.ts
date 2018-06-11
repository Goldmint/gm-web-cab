import {ChangeDetectorRef, Component, OnInit, ViewChild} from '@angular/core';
import {APIService, MessageBoxService} from "../../services";

@Component({
  selector: 'app-fees-page',
  templateUrl: './fees-page.component.html',
  styleUrls: ['./fees-page.component.sass']
})
export class FeesPageComponent implements OnInit {

  @ViewChild('addCryptoItem') formCryptoItem;
  @ViewChild('addFiatItem') formFiatItem;

  public loading = false;
  public isDataLoaded = false;
  public currencyTypeList = ['fiat', 'crypto']
  public currentCurrencyType = this.currencyTypeList[0];
  public selectedCurrency: string;
  public isNewCurrency: boolean = false;
  public showAddNewItemForm: boolean = false;
  public isFeesEmpty: boolean = false;
  public fees: object;
  public newFiatFees = {
    name: '',
    methods: [
      {
        name: '',
        deposit: '',
        withdraw: ''
      }
    ]
  };
  public newCryptoFees = {
    name: '',
    methods: [
      {
        name: '',
        deposit: '',
        withdraw: ''
      }
    ]
  };

  constructor(private _cdRef: ChangeDetectorRef,
              private apiService: APIService,
              private _messageBox: MessageBoxService) { }

  ngOnInit() {
    this.apiService.getFees().subscribe(data => {
      this.fees = data.data;

      if (this.fees['crypto'].length === 0 || this.fees['fiat'].length === 0) {
        this.isFeesEmpty = true;
      }

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

  onSelectCurrency() {
    this.isNewCurrency = this.selectedCurrency === 'new' ? true : false;
  }

  addFiatFess() {
    if (this.isNewCurrency || this.isFeesEmpty) {
      let isFind = false;
      this.fees[this.currencyTypeList[0]].forEach((item) => {
        this.newFiatFees.name === item.name && (isFind = true);
      });

      if (!isFind) {
        const obj = JSON.stringify(this.newFiatFees);
        this.fees[this.currencyTypeList[0]].push(JSON.parse(obj));
      } else {
        this._messageBox.alert('This currency name is already used');
      }
    }

    if (!this.isNewCurrency && !this.isFeesEmpty) {
      this.fees[this.currencyTypeList[0]].forEach((item) => {
        if (item.name === this.selectedCurrency) {
          item.methods.push(this.newFiatFees.methods[0]);
        }
      });
    }
    !this.isFeesEmpty && this.updateFees();
  }

  addCryptoFees() {
    let isFind = false;
    this.fees[this.currencyTypeList[1]].forEach((item) => {
      this.newCryptoFees.name === item.name && (isFind = true);
    });

    if (!isFind) {
      this.newCryptoFees.methods[0].name = this.newCryptoFees.name;
      const obj = JSON.stringify(this.newCryptoFees);
      this.fees[this.currencyTypeList[1]].push(JSON.parse(obj));
    } else {
      this._messageBox.alert('This currency name is already used');
    }
    !this.isFeesEmpty && this.updateFees();
  }

  addAllFees() {
    this.addFiatFess();
    this.addCryptoFees();
    this.updateFees();
  }

  updateFees() {
    this.loading = true;

    this.apiService.updateFees(this.fees).subscribe(() => {
      this._messageBox.alert('Changes saved');
      this.formCryptoItem.reset();
      this.formFiatItem.reset();
      this.apiService.getFees()
        .finally(() => {
          this.loading = false;
          this._cdRef.markForCheck();
        })
        .subscribe((data) => {
          this.fees = data.data;
      });
    }, error => {
      this._messageBox.alert('Error. Something went wrong.');
    });
  }

}
