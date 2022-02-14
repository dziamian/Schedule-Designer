import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { UserInfo, SearchUser } from 'src/app/others/Accounts';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji wprowadzania i modyfikowania danych na temat użytkowników.
 */
@Component({
  selector: 'app-user-field',
  templateUrl: './user-field.component.html',
  styleUrls: ['./user-field.component.css']
})
export class UserFieldComponent implements OnInit {

  /** Liczba odnalezionych użytkowników wyświetlanych na pojedynczej stronie. */
  readonly PAGE_SIZE: number = 5;

  /** 
   * Dane wymagane do załadowania widoku obszaru roboczego.
   * Zawiera informacje o identyfikatorze zasobu, typie zasobu oraz rodzaju wykonywanej akcji (dodawania lub podglądu).
   */
  private _data: {id: string|undefined, type: string, actionType: string};

  /**
   * Metoda ustawiająca dane wymagane do załadowania widoku obszaru roboczego.
   * Po ustawieniu danych następuje załadowanie widoku.
   */
  @Input() set data(value: {id: string|undefined, type: string, actionType: string}) {
    this._data = value;
    this.loadView();
  } get data(): {id: string|undefined, type: string, actionType: string} {
    return this._data;
  }

  /** 
   * Emiter zdarzenia dodania nowego zasobu do systemu.
   * Zdarzenie przechowuje informacje o identyfikatorach powstałego zasobu.
  */
  @Output() onCreate: EventEmitter<string> = new EventEmitter();
  /** Emiter zdarzenia usunięcia zasobu z systemu. */
  @Output() onRemove: EventEmitter<void> = new EventEmitter();
  /** Emiter zdarzenia zapisu stanu modyfikacji zasobu. */
  @Output() onRefresh: EventEmitter<void> = new EventEmitter();

  /** Informacje o pobranym zasobie użytkownika z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalUserInfo: UserInfo;

  /** Wartości początkowe formularza modyfikacji zasobu w celu 
   * możliwości późniejszego ich zresetowania. */
  originalValues: any;
  /** Określa czy włączony został tryb modyfikacji zasobu. */
  isModifying: boolean = false;

  /** Informuje czy dane zostały załadowane. */
  loading: boolean | null = null;
  /** Informuje czy rezultaty wyszukiwania zostały załadowane. */
  resultsLoading: boolean = false;

  constructor(
    private administratorApiService: AdministratorApiService,
    private snackBar: MatSnackBar
  ) { }

  /** Wynik wyszukiwania użytkowników w systemie USOS. */
  searchResults: SearchUser | null;
  /** Określa ilu użytkowników należy pominąć, aby uzyskać odpowiednią stronę rezultatów. */
  searchStart: number = 0;
  /** Aktualnie wyświetlona strona. */
  currentPage: number = 0;
  /** Maksymalna liczba istniejących stron rezultatów, o których wiadomo w danym momencie. */
  maxPage: number = 0;
  /** Kryteria wyszukiwania użytkowników. */
  currentQuery: string;

  /** Formularz wyszukiwania użytkowników w systemie USOS. */
  searchForm: FormGroup;
  /** Formularz modyfikacji zasobu. */
  userInfoForm: FormGroup;

  ngOnInit(): void {
  }

  /**
   * Metoda budująca formularz wyszukiwania użytkowników.
   */
  private buildSearchForm() {
    this.searchForm = new FormGroup({
      search: new FormControl('', [Validators.required])
    });
  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param userInfo Dane początkowe zbudowanego formularza
   */
  private buildAccountForm(userInfo: UserInfo) {
    this.userInfoForm = new FormGroup({
      firstName: new FormControl(userInfo.FirstName, [Validators.required]),
      lastName: new FormControl(userInfo.LastName, [Validators.required]),
      academicNumber: new FormControl(userInfo.AcademicNumber ?? ''),
      titleBefore: new FormControl(userInfo.TitleBefore ?? ''),
      titleAfter: new FormControl(userInfo.TitleAfter ?? ''),
      staff: new FormControl(userInfo.IsStaff),
      coordinator: new FormControl(userInfo.IsCoordinator),
      admin: new FormControl(userInfo.IsAdmin),
      student: new FormControl(userInfo.IsStudent)
    });
    this.originalValues = this.userInfoForm.value;
  }

  /** Metoda wyłączająca możliwość modyfikacji formularzy. */
  private disableForm() {
    for (var controlName in this.userInfoForm.controls) {
      this.userInfoForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularzy. */
  private enableForm() {
    for (var controlName in this.userInfoForm.controls) {
      this.userInfoForm.controls[controlName].enable();
    }
    this.isModifying = true;
  }

  /**
   * Metoda ładująca dane wymagane do wyświetlenia obszaru roboczego.
   * Różnią się one w zależności od trybu widoku - dodawania lub podglądu.
   */
  private loadView() {
    this.loading = true;

    if (this._data.actionType === 'view') {
      this.administratorApiService.GetUserAccount(Number.parseInt(this._data.id!)).subscribe(userInfo => {
        this.originalUserInfo = userInfo;

        this.buildAccountForm(this.originalUserInfo);

        this.disableForm();

        this.loading = false;
      }, () => {
        this.snackBar.open("Could not find user.", "OK");
      });
    } else if (this._data.actionType === 'add') {
      this.searchResults = null;
      this.searchStart = 0;
      this.currentQuery = '';
      
      this.buildSearchForm();
      
      this.loading = false;
    } else {
      this.loading = false;
    }
  }
  
  /**
   * Metoda porównująca aktualny stan pól formularzy z oryginalnymi 
   * wartościami zasobu pobranymi z serwera.
   * @returns Prawdę jeśli dane w formularzu są identyczne z oryginalnymi wartościami zasobu
   */
  IsSameAsOriginal(): boolean {
    return this.originalUserInfo.FirstName === this.userInfoForm.controls['firstName'].value
      && this.originalUserInfo.LastName === this.userInfoForm.controls['lastName'].value
      && this.originalUserInfo.AcademicNumber === this.userInfoForm.controls['academicNumber'].value
      && this.originalUserInfo.TitleBefore === this.userInfoForm.controls['titleBefore'].value
      && this.originalUserInfo.TitleAfter === this.userInfoForm.controls['titleAfter'].value
      && this.originalUserInfo.IsStaff === this.userInfoForm.controls['staff'].value
      && ((this.userInfoForm.controls['staff'].value
        && this.originalUserInfo.IsAdmin === this.userInfoForm.controls['admin'].value
        && this.originalUserInfo.IsCoordinator === this.userInfoForm.controls['coordinator'].value)
        || !this.userInfoForm.controls['staff'].value)
      && this.originalUserInfo.IsStudent === this.userInfoForm.controls['student'].value;
  }

  /**
   * Metoda uruchamiająca tryb modyfikacji zasobu.
   */
  Modify() {
    this.Reset();
    this.enableForm();
  }

  Reset() {
    this.userInfoForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  async Save() {
    if (!this.userInfoForm.valid) {
      return;
    }

    const firstName = this.userInfoForm.controls['firstName'].value;
    const lastName = this.userInfoForm.controls['lastName'].value;
    const academicNumber = this.userInfoForm.controls['academicNumber'].value;
    const titleBefore = this.userInfoForm.controls['titleBefore'].value;
    const titleAfter = this.userInfoForm.controls['titleAfter'].value;
    const staff = this.userInfoForm.controls['staff'].value;
    const coordinator = staff ? this.userInfoForm.controls['coordinator'].value : undefined;
    const admin = staff ? this.userInfoForm.controls['admin'].value : undefined;
    const student = this.userInfoForm.controls['student'].value;

    const userInfo = {
      UserId: this.originalUserInfo.UserId,
      FirstName: this.originalUserInfo.FirstName === firstName ? undefined : firstName,
      LastName: this.originalUserInfo.LastName === lastName ? undefined : lastName,
      AcademicNumber: this.originalUserInfo.AcademicNumber === academicNumber ? undefined : academicNumber,
      TitleBefore: this.originalUserInfo.TitleBefore === titleBefore ? undefined : titleBefore,
      TitleAfter: this.originalUserInfo.TitleAfter === titleAfter ? undefined : titleAfter,
      IsStudent: this.originalUserInfo.IsStudent === student ? undefined : student,
      IsStaff: this.originalUserInfo.IsStaff === staff ? undefined : staff,
      IsCoordinator: this.originalUserInfo.IsCoordinator === coordinator ? undefined : coordinator,
      IsAdmin: this.originalUserInfo.IsAdmin === admin ? undefined : admin,
    }

    this.disableForm();
    this.administratorApiService.UpdateUser(userInfo).subscribe(() => {
      this.onRefresh.emit();
      
      this.snackBar.open("Successfully updated user.", "OK");

      this.loadView();
    }, response => {
      this.enableForm();
      
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie usunięcia zasobu na serwer.
   */
  Remove() {
    this.administratorApiService.RemoveUser(this.originalUserInfo.UserId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed user.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie utworzenia nowego konta użytkownika na podstawie identyfikatora w systemie USOS.
   * @param userId Identyfikator użytkownika w systemie USOS
   */
  Create(userId: number) {
    this.administratorApiService.CreateAccountFromUsos(userId).subscribe(response => {
      this.onCreate.emit(`${userId}`);
      
      this.snackBar.open("Successfully created account for this user.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie wyszukania użytkowników spełniających 
   * kryteria podanych w polu formularza na serwer. Wyświetlona zostanie pierwsza strona
   * rezultatów.
   */
  Search() {
    if (!this.searchForm.valid) {
      return;
    }
    if (this.currentQuery === this.searchForm.controls['search'].value) {
      return;
    }

    this.resultsLoading = true;
    this.searchResults = null;

    this.currentQuery = this.searchForm.controls['search'].value;

    this.administratorApiService.SearchForUserFromUsos(
      this.currentQuery, this.PAGE_SIZE, 0
    ).pipe(finalize(() => {
      this.resultsLoading = false;
    })).subscribe(result => {
      this.searchResults = result;
      this.searchStart = 0;
      this.currentPage = 1;
      this.maxPage = this.searchResults.NextPage ? this.currentPage + 1 : this.currentPage;
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie wyszukania użytkowników spełniających 
   * kryteria podanych w polu formularza na serwer. Wyświetlona zostanie kolejna strona
   * rezultatów.
   */
  async SearchNextPage() {
    this.resultsLoading = true;
    this.searchResults = null;

    try {
      const result = await this.administratorApiService.SearchForUserFromUsos(
        this.currentQuery, this.PAGE_SIZE, this.searchStart + this.PAGE_SIZE
      ).toPromise();

      this.searchResults = result;
      this.searchStart += this.PAGE_SIZE;
      ++this.currentPage;
      if (this.maxPage <= this.currentPage) {
        this.maxPage = this.searchResults.NextPage ? this.currentPage + 1 : this.currentPage;
      }
    } catch (response : any) {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    }
    this.resultsLoading = false;
  }

  /**
   * Metoda wysyłająca żądanie wyszukania użytkowników spełniających 
   * kryteria podanych w polu formularza na serwer. Wyświetlona zostanie poprzednia strona
   * rezultatów.
   */
  async SearchPreviousPage() {
    this.resultsLoading = true;
    this.searchResults = null;

    try {
      const result = await this.administratorApiService.SearchForUserFromUsos(
        this.currentQuery, this.PAGE_SIZE, this.searchStart - this.PAGE_SIZE
      ).toPromise();

      this.searchResults = result;
      this.searchStart -= this.PAGE_SIZE;
      --this.currentPage;
    } catch (response : any) {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    }
    this.resultsLoading = false;
  }
}
