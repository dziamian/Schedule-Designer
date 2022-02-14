import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { UserInfo, Coordinator, CoordinatorBasic, Titles, User } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType, RoomType } from 'src/app/others/Types';
import { Group, GroupInfo } from 'src/app/others/Group';
import { Room } from 'src/app/others/Room';
import { ScheduleSlot } from 'src/app/others/ScheduleSlot';
import { Settings } from 'src/app/others/Settings';
import { CourseEditionInfo, CourseInfo } from 'src/app/others/CourseInfo';
import { ScheduledMove, ScheduledMoveDetails, ScheduledMoveInfo } from 'src/app/others/ScheduledMove';
import { Filter } from 'src/app/others/Filter';
import { Course } from 'src/app/others/Course';
import { environment } from 'src/environments/environment';

/**
 * Serwis odpowiadający za wysyłanie podstawowych żądań do API serwera.
 */
@Injectable({
  providedIn: 'root'
})
export class ScheduleDesignerApiService {

  /** Bazowy adres URL API serwera. */
  readonly baseUrl:string = environment.baseApiUrl;

  constructor(private http:HttpClient) { }

  /**
   * Pobranie nagłówka autoryzującego.
   * @param token Token dostępu
   * @returns Nagłówek autoryzujący
   */
  private GetAuthorizationHeaders(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o koncie zalogowanego użytkownika do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetMyAccount():Observable<UserInfo> {
    const request = {
      url: this.baseUrl + '/users/Service.GetMyAccount()?$expand=Groups',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => 
      new UserInfo(
        response.UserId,
        response.FirstName,
        response.LastName,
        response.AcademicNumber,
        response.TitleBefore,
        response.TitleAfter,
        response.IsStudent,
        response.IsStaff,
        response.IsCoordinator,
        response.IsAdmin,
        response.Groups.filter((group : any) => group.IsRepresentative).map((group : any) => group.GroupId) ?? []
      )
    ));
  }

  /**
   * Metoda wysyłająca żądanie utworzenia konta zalogowanego użytkownika do API systemu.
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
  public CreateMyAccount():Observable<any> {
    const request = {
      url: this.baseUrl + '/users/Service.CreateMyAccount',
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o ustawieniach aplikacji do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetSettings():Observable<Settings> {
    const request = {
      url: this.baseUrl + '/settings',
      method: 'GET'
    };
    
    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => new Settings(
      response.CourseDurationMinutes,
      response.StartTime,
      response.EndTime,
      response.TermDurationWeeks
    )));
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o typach przedmiotów do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (kolekcję typów przedmiotów i ich identyfikatorów)
   */
  public GetCourseTypes():Observable<Map<number,CourseType>> {
    const request = {
      url: this.baseUrl + '/courseTypes',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let map = new Map<number,CourseType>();
        
        response.value.forEach((element : any) => {
          map.set(element.CourseTypeId, new CourseType(element.CourseTypeId, element.Name, element.Color));
        });
        
        return map;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o typie przedmiotu do API systemu.
   * @param id ID żądanego typu przedmiotu
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetCourseType(id: number): Observable<CourseType> {
    const request = {
      url: this.baseUrl + `/courseTypes(${id})`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new CourseType(response.CourseTypeId, response.Name, response.Color))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o przedmiocie do API systemu.
   * @param id ID żądanego przedmiotu
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetCourse(id: number): Observable<Course> {
    const request = {
      url: this.baseUrl + `/courses(${id})?$expand=CourseType`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new Course(response.CourseId, new CourseType(
        response.CourseType.CourseTypeId, 
        response.CourseType.Name,
        response.CourseType.Color), response.Name, response.UnitsMinutes))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o typie pokoju do API systemu.
   * @param roomTypeId ID żądanego typu pokoju
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetRoomType(roomTypeId: number):Observable<RoomType> {
    const request = {
      url: this.baseUrl + `/roomTypes(${roomTypeId})`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new RoomType(response.RoomTypeId, response.Name))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o typach pokojów do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (kolekcję typów pokojów i ich identyfikatorów)
   */
  public GetRoomTypes():Observable<Map<number,RoomType>> {
    const request = {
      url: this.baseUrl + '/roomTypes',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let map = new Map<number,RoomType>();
        
        response.value.forEach((element : any) => {
          map.set(element.RoomTypeId, new RoomType(element.RoomTypeId, element.Name));
        });
        
        return map;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o etykietach istniejących ram czasowych do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę etykiet)
   */
  public GetPeriods():Observable<string[]> {
    const request = {
      url: this.baseUrl + '/settings/Service.GetPeriods()',
      method: 'GET'
    };
    
    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value)
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji na temat statusu blokady edycji zajęć do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param courseEditionId ID żądanej edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (wartość blokady oraz informację czy blokada została nałożona przez administratora)
   */
  public IsCourseEditionLocked(courseId:number, courseEditionId:number):Observable<{value: boolean, byAdmin: boolean}> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})?$expand=LockUser`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        return {
          value: response.LockUserId != null, byAdmin: response.LockUser?.IsAdmin
        };
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o istniejących prowadzących do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę prowadzących)
   */
  public GetCoordinators(): Observable<Coordinator[]> {
    const request = {
      url: this.baseUrl + `/users/Service.GetCoordinators()`,
      method: 'GET'
    }

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((value : any) => 
        new Coordinator(
          new User(
            value.UserId,
            value.FirstName,
            value.LastName,
          ),
          new Titles(
            value.TitleBefore,
            value.TitleAfter
          )
        ))
      )
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o istniejących prowadzących wśród podanych użytkowników do API systemu.
   * @param usersIds Identyfikatory użytkowników wśród których należy szukać prowadzących
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę prowadzących)
   */
  public GetCoordinatorsFromUsers(usersIds:number[]):Observable<Coordinator[]> {
    const request = {
      url: this.baseUrl + `/users?$filter=UserId in [${usersIds.toString()}]`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((value : any) => 
        new Coordinator(
          new User(
            value.UserId,
            value.FirstName,
            value.LastName
          ),
          new Titles(
            value.TitleBefore,
            value.TitleAfter
          )
        ))
      )
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania skróconych informacji o edycji zajęć do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param courseEditionId ID żądanej edycji zajęć
   * @param settings Ustawienia aplikacji
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetCourseEditionInfo(
    courseId:number, 
    courseEditionId:number, 
    settings:Settings
  ):Observable<CourseEditionInfo> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})?`
      + `$expand=Course,LockUser,SchedulePositions($count=true;$top=0)`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const courseEditionInfo = new CourseEditionInfo(
          courseId, courseEditionId, response.Course.CourseTypeId, response.Course.Name, response.Name
        );
        courseEditionInfo.UnitsMinutes = response.Course.UnitsMinutes;
        courseEditionInfo.ScheduleAmount = response['SchedulePositions@odata.count'];
        courseEditionInfo.FullAmount = courseEditionInfo.UnitsMinutes / settings.CourseDurationMinutes;
        courseEditionInfo.IsLocked = response.LockUserId != null;
        courseEditionInfo.IsLockedByAdmin = !!response.LockUser?.IsAdmin;
        return courseEditionInfo;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania podstawowych informacji o edycji zajęć do API systemu (nazwa przedmiotu, edycji zajęć oraz identyfikator typu przedmiotu).
   * @param courseId ID żądanego przedmiotu
   * @param courseEditionId ID żądanej edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetCourseEditionBasicInfo(
    courseId: number,
    courseEditionId: number
  ):Observable<CourseEditionInfo> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})?$expand=Course`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const courseEditionInfo = new CourseEditionInfo(
          response.CourseId, response.CourseEditionId, response.Course.CourseTypeId, response.Course.Name, response.Name
        );
        return courseEditionInfo;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania skróconych informacji o przedmiotach do API systemu (nazwa przedmiotu oraz identyfikator typu).
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę skróconych informacji)
   */
  public GetCoursesInfo():Observable<CourseInfo[]> {
    const request = {
      url: this.baseUrl + `/courses`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => {
        const courseInfo = new CourseInfo(
          element.CourseId, element.CourseTypeId, element.Name
        );
        return courseInfo;
      }))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania skróconych informacji o edycjach zajęć do API systemu.
   * @param settings Ustawienia aplikacji
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę skróconych informacji)
   */
  public GetCourseEditionsInfo(settings:Settings):Observable<CourseEditionInfo[]> {
    const request = {
      url: this.baseUrl + `/courseEditions`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => {
        const courseEditionInfo = new CourseEditionInfo(
          element.CourseId, element.CourseEditionId, -1, '', element.Name
        );
        return courseEditionInfo;
      }))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o edycji zajęć spełniającej kryteria podanego filtra do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param courseEditionId ID żądanej edycji zajęć
   * @param frequency Maksymalna liczba jednostek zajęciowych możliwych do ustawienia na planie
   * @param filter Kryteria wyszukiwania
   * @param courseTypes Kolekcja typów przedmiotów
   * @param settings Ustawienia aplikacji
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę edycji zajęć)
   */
  public GetFilteredCourseEdition(
    courseId:number,
    courseEditionId:number,
    frequency:number, 
    filter:Filter,
    courseTypes:Map<number,CourseType>, 
    settings:Settings
  ):Observable<CourseEdition[]> {
    const FREQUENCY = Math.floor(frequency);
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.GetMyCourseEdition(${filter.toString()},Frequency=${FREQUENCY})?` +
        '$expand=Course,Groups,Coordinators,LockUser,' +
        'SchedulePositions($count=true;$top=0)',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let myCourseEditions = new Array<CourseEdition>();
        
        let groups = new Array<Group>();
        response.Groups.forEach((element : any) => {
          groups.push(new Group(
            element.GroupId
          ));
        });
        let coordinators = new Array<Coordinator>();
        response.Coordinators.forEach((element : any) => {
          coordinators.push(new Coordinator(
            new User(
              element.Coordinator.UserId,
              element.Coordinator.FirstName,
              element.Coordinator.LastName
            ),
            new Titles(
              element.Coordinator.TitleBefore,
              element.Coordinator.TitleAfter
            )
          ));
        });
        const scheduleAmount = response['SchedulePositions@odata.count'];
        const fullAmount = response.Course.UnitsMinutes / settings.CourseDurationMinutes;
        const fullAmountInteger = Math.ceil(fullAmount);
        const coursesAmount = Math.floor((fullAmountInteger - scheduleAmount) / FREQUENCY);
        for (let i = 0; i < coursesAmount; ++i) {
          let courseEdition = new CourseEdition(
            response.CourseId, response.CourseEditionId,
            response.Course.Name, courseTypes.get(response.Course.CourseTypeId) ?? new CourseType(0, "", ""),
            FREQUENCY,
            groups, coordinators
          );
          courseEdition.IsLocked = response.LockUserId;
          courseEdition.IsLockedByAdmin = !!response.LockUser?.IsAdmin;
          courseEdition.ScheduleAmount = scheduleAmount;
          courseEdition.FullAmount = fullAmount;
          myCourseEditions.push(courseEdition);
        }

        return myCourseEditions;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o edycjach zajęć spełniających kryteria podanego filtra do API systemu.
   * @param frequency Maksymalna liczba jednostek zajęciowych możliwych do ustawienia na planie
   * @param filter Kryteria wyszukiwania
   * @param courseTypes Kolekcja typów przedmiotów
   * @param settings Ustawienia aplikacji
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę edycji zajęć)
   */
  public GetFilteredCourseEditions(
    frequency:number,
    filter:Filter,
    courseTypes:Map<number,CourseType>, 
    settings:Settings
  ):Observable<CourseEdition[]> {
    const FREQUENCY = Math.floor(frequency);
    const request = {
      url: this.baseUrl + `/courseEditions/Service.GetFilteredCourseEditions(${filter.toString()},Frequency=${FREQUENCY})?` +
        '$expand=Course,LockUser,' +
        'SchedulePositions($count=true;$top=0)',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let myCourseEditions = new Array<CourseEdition>();

        response.value.forEach((value : any) => {
          const scheduleAmount = value['SchedulePositions@odata.count'];
          const fullAmount = value.Course.UnitsMinutes / settings.CourseDurationMinutes;
          const fullAmountInteger = Math.ceil(fullAmount);
          const coursesAmount = Math.floor((fullAmountInteger - scheduleAmount) / FREQUENCY);
          for (let i = 0; i < coursesAmount; ++i) {
            let courseEdition = new CourseEdition(
              value.CourseId, value.CourseEditionId,
              value.Course.Name, 
              courseTypes.get(value.Course.CourseTypeId) ?? new CourseType(0, "", ""),
              FREQUENCY,
              [], 
              []
            );
            courseEdition.IsLocked = value.LockUserId;
            courseEdition.IsLockedByAdmin = !!value.LockUser?.IsAdmin;
            courseEdition.ScheduleAmount = scheduleAmount;
            courseEdition.FullAmount = fullAmount;
            myCourseEditions.push(courseEdition);
          }
        });

        return myCourseEditions;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o rozmiarze grup przypisanych do danej edycji zajęć do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param courseEditionId ID żądanej edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetCourseEditionGroupsSize(courseId:number, courseEditionId:number):Observable<number> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.GetCourseEditionGroupsSize()`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value)
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji na temat statusu blokady pozycji w planie do API systemu.
   * @param roomId Identyfikator pokoju
   * @param periodIndex Indeks okienka czasowego w ciągu dnia
   * @param day Indeks dnia tygodnia
   * @param weeks Tygodnie, które będą brane pod uwagę
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (wartość blokady oraz informację czy blokada została nałożona przez administratora)
   */
  public AreSchedulePositionsLocked(
    roomId:number, periodIndex:number,
    day:number, weeks:number[]
  ):Observable<{value: boolean, byAdmin: boolean}> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetSchedulePositions(`
      + `RoomId=${roomId},PeriodIndex=${periodIndex},Day=${day},Weeks=[${weeks.toString()}])?$expand=LockUser`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        return {
          value: response.value.find((x:any) => x.LockUserId != null) != undefined,
          byAdmin: response.value.find((x:any) => x.LockUser?.IsAdmin) != undefined
        }
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania skróconych informacji o przedmiotach do API systemu (nazwy i wymaganej liczby jednostek zajęciowych do odbycia w semestrze).
   * Modyfikuje tablicę edycji zajęć poprzez wypełnienie istniejących obiektów nowymi informacjami. 
   * @param courseEditions Posortowana tablica edycji zajęć względem ID przedmiotu
   * @param courseTypes Kolekcja typów przedmiotów
   * @param settings Ustawienia aplikacji
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (zmodyfikowaną tablicę edycji zajęć)
   */
  public GetCoursesForSchedule(
    courseEditions: CourseEdition[],
    courseTypes:Map<number,CourseType>,
    settings:Settings
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/courses?$filter=CourseId in (${[...new Set(courseEditions.map(c => c.CourseId))]})&$orderby=CourseId`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        var j = 0;

        const courseEditionsLength = courseEditions.length;
        for (var i = 0; i < courseEditionsLength; ++i) {
          if (response.value[j].CourseId != courseEditions[i].CourseId) {
            ++j;
          }

          if (response.value[j].CourseId == courseEditions[i].CourseId) {
            courseEditions[i].Name = response.value[j].Name;
            courseEditions[i].Type = courseTypes.get(response.value[j].CourseTypeId) ?? new CourseType(0, "", "");
            courseEditions[i].FullAmount = response.value[j].UnitsMinutes / settings.CourseDurationMinutes;
          }
        }

        return courseEditions;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania liczby ułożonych zajęć w planie do API systemu.
   * Modyfikuje tablicę edycji zajęć poprzez wypełnienie istniejących obiektów nowymi informacjami. 
   * @param courseEditions Posortowana tablica edycji zajęć względem ID edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (zmodyfikowaną tablicę edycji zajęć)
   */
  public GetScheduleAmount(
    courseEditions: CourseEdition[]
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetScheduleAmount(CourseEditionIds=[${[...new Set(courseEditions.map(c => c.CourseEditionId))]}])?$orderby=CourseEditionId`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        var j = 0;

        const courseEditionsLength = courseEditions.length;
        for (var i = 0; i < courseEditionsLength; ++i) {
          if (response.value[j].CourseEditionId != courseEditions[i].CourseEditionId) {
            ++j;
          }

          if (response.value[j].CourseEditionId == courseEditions[i].CourseEditionId) {
            courseEditions[i].ScheduleAmount = response.value[j].Count;
          }
        }

        return courseEditions;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania podstawowych informacji o prowadzących edycję zajęć do API systemu.
   * @param courseEditionId ID żądanej edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę podstawowych informacji o prowadzących)
   */
  public GetCourseEditionCoordinatorsBasic(courseEditionId: number):Observable<CoordinatorBasic[]> {
    const request = {
      url: this.baseUrl + `/coordinatorCourseEditions?$expand=User&$filter=CourseEditionId eq ${courseEditionId}`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new CoordinatorBasic(
        element.User.UserId,
        `${element.User.TitleBefore ?? ''} ${element.User.LastName.toUpperCase()} ${element.User.FirstName} ${element.User.TitleAfter ?? ''} (${element.User.UserId})`
      )))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o prowadzących do API systemu.
   * Modyfikuje tablicę edycji zajęć poprzez wypełnienie istniejących obiektów nowymi informacjami. 
   * @param courseEditions Posortowana tablica edycji zajęć względem ID edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (zmodyfikowaną tablicę edycji zajęć)
   */
  public GetCoordinatorsForCourses(
    courseEditions: CourseEdition[]
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/coordinatorCourseEditions?$expand=User&$filter=CourseEditionId in (${[...new Set(courseEditions.map(c => c.CourseEditionId))]})&$orderby=CourseEditionId`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const length = response.value.length;
        if (length == 0) {
          return courseEditions;
        }

        for (var i = 0; i < length; ++i) {
          for (var key in courseEditions) {
            if (courseEditions[key].CourseEditionId == response.value[i].CourseEditionId) {
              courseEditions[key].Coordinators.push(new Coordinator(
                new User(
                  response.value[i].User.UserId,
                  response.value[i].User.FirstName,
                  response.value[i].User.LastName
                ),
                new Titles(
                  response.value[i].User.TitleBefore,
                  response.value[i].User.TitleAfter
                )));
            }
          }
        }

        return courseEditions;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania podstawowych informacji o grupach przypisanych do edycji zajęć do API systemu.
   * @param courseEditionId ID żądanej edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę podstawowych informacji o grupach)
   */
  public GetCourseEditionGroupsBasic(courseEditionId: number):Observable<Group[]> {
    const request = {
      url: this.baseUrl + `/groupCourseEditions?$filter=CourseEditionId eq ${courseEditionId}`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new Group(
        element.GroupId
      )))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o grupach do API systemu.
   * Modyfikuje tablicę edycji zajęć poprzez wypełnienie istniejących obiektów nowymi informacjami. 
   * @param courseEditions Posortowana tablica edycji zajęć względem ID edycji zajęć
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (zmodyfikowaną tablicę edycji zajęć)
   */
  public GetGroupsForCourses(
    courseEditions: CourseEdition[]
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/groupCourseEditions?$filter=CourseEditionId in (${[...new Set(courseEditions.map(c => c.CourseEditionId))]})&$orderby=CourseEditionId`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const length = response.value.length;
        if (length == 0) {
          return courseEditions;
        }

        for (var i = 0; i < length; ++i) {
          for (var key in courseEditions) {
            if (courseEditions[key].CourseEditionId == response.value[i].CourseEditionId) {
              courseEditions[key].Groups.push(new Group(response.value[i].GroupId));
            }
          }
        }

        return courseEditions;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o pozycjach w planie spełniających kryteria podanego filtra do API systemu.
   * @param weeks Tygodnie, które mają być brane pod uwagę
   * @param filter Kryteria wyszukiwania
   * @param settings Ustawienia aplikacji
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane 
   * żądanie (trójwymiarową tablicę planu z edycjami zajęć oraz jednowymiarową tablicę zawierającą te edycje)
   */
  public GetFilteredSchedule(
    weeks:number[],
    filter: Filter,
    settings:Settings
  ):Observable<{schedule: CourseEdition[][][], courseEditions: CourseEdition[]}> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetFilteredSchedule(${filter.toString()},Weeks=[${weeks.toString()}])?
        $expand=Timestamp,LockUser,ScheduledMovePositions($expand=ScheduledMove($select=MoveId,UserId,IsConfirmed))`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const numberOfSlots = settings.Periods.length - 1;
        
        let schedule:CourseEdition[][][] = [];
        for (let j:number = 0; j < 5; ++j) {
          schedule.push([]);
          for (let i:number = 0; i < numberOfSlots; ++i) {
            schedule[j].push([]);
          }
        }

        let courseEditions: CourseEdition[] = [];

        response.value.forEach((value : any) => {
          
          const courseId = value.CourseId;
          const courseEditionId = value.CourseEditionId;
          const roomId = value.RoomId;
          const dayIndex = value.Timestamp.Day - 1;
          const periodIndex = value.Timestamp.PeriodIndex - 1;
          const week = value.Timestamp.Week;
          const locked = {value: value.LockUserId != null, byAdmin: !!value.LockUser?.IsAdmin};
          const scheduledMoves = value.ScheduledMovePositions.map((value : any) => new ScheduledMove(value.ScheduledMove.MoveId, value.ScheduledMove.UserId, value.ScheduledMove.IsConfirmed));

          let scheduleSlot = schedule[dayIndex][periodIndex];
          let found = false;
          for (let i = 0; i < scheduleSlot.length; ++i) {
            let courseEdition = scheduleSlot[i];
            if (courseEdition.CourseId == courseId && courseEdition.CourseEditionId == courseEditionId
              && courseEdition.Room!.RoomId == roomId) {
                courseEdition.Weeks?.push(week);
                if (locked) {
                  courseEdition.IsLocked = locked.value;
                  courseEdition.IsLockedByAdmin = locked.byAdmin;
                }
                const currentMovesIds = courseEdition.ScheduledMoves.map(scheduledMove => scheduledMove.MoveId);
                const notAddedScheduledMoves = scheduledMoves.filter((scheduledMove : ScheduledMove) => !currentMovesIds.includes(scheduledMove.MoveId));
                courseEdition.ScheduledMoves.push(...notAddedScheduledMoves);

                found = true;
              }
          }
          if (!found) {
            const courseEdition = new CourseEdition(
              value.CourseId, 
              value.CourseEditionId,
              "",
              new CourseType(0, "", ""),
              0,
              [],
              []
            );
            courseEdition.Room = new Room(roomId);
            courseEdition.IsLocked = locked.value;
            courseEdition.IsLockedByAdmin = locked.byAdmin;
            courseEdition.Weeks = [week];
            courseEdition.ScheduledMoves.push(...scheduledMoves);
            
            scheduleSlot.push(courseEdition);
            courseEditions.push(courseEdition);
          }
        });

        return {schedule: schedule, courseEditions: courseEditions};
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o pokoju do API systemu.
   * @param roomTypeId ID żądanego pokoju
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetRoom(id: number): Observable<Room> {
    const request = {
      url: this.baseUrl + `/rooms(${id})?$expand=Type`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const room = new Room(response.RoomId);
        room.Name = response.Name;
        room.RoomType = new RoomType(
          response.Type.RoomTypeId, 
          response.Type.Name
        );
        room.Capacity = response.Capacity;
        return room;
      })
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o pokojach do API systemu.
   * @param roomsTypes Kolekcja typów pokojów
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę pokojów)
   */
  public GetRooms(roomsTypes:Map<number,RoomType> = new Map<number, RoomType>()):Observable<Room[]> {
    const request = {
      url: this.baseUrl + `/rooms`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) =>
        response.value.map((element : any) => {
          const room = new Room(element.RoomId);
          room.Name = element.Name;
          room.Capacity = element.Capacity;
          room.RoomType = roomsTypes.get(element.RoomTypeId) ?? new RoomType(element.RoomTypeId, "");
          return room;
        })
      )
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o nazwach pokojów do API systemu.
   * @param roomsIds Identyfikatory pokojów
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę nazw pokojów)
   */
  public GetRoomsNames(roomsIds:number[]):Observable<string[]> {
    const request = {
      url: this.baseUrl + `/rooms/Service.GetRoomsNames(RoomsIds=[${roomsIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => element.Name))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o pokojach przypisanych do danego przedmiotu do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param roomsTypes Kolekcja typów pokojów
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę pokojów)
   */
  public GetCourseRooms(courseId:number, roomsTypes:Map<number,RoomType>):Observable<Room[]> {
    const request = {
      url: this.baseUrl + `/courseRooms?$expand=Room,User&$filter=CourseId eq ${courseId}`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) =>
        response.value.map((element : any) => {
          const room = new Room(element.RoomId);
          room.Name = element.Room.Name;
          room.Capacity = element.Room.Capacity;
          room.RoomType = roomsTypes.get(element.Room.RoomTypeId) ?? new RoomType(element.Room.RoomTypeId, "");
          if (element.User != null) {
            room.User = new User(element.User.UserId, element.User.FirstName, element.User.LastName);
          }
          return room;
        })
      )
    );
  }
  
  /**
   * Metoda wysyłająca żądanie pobrania informacji na temat dostępności pokojów w określonych ramach czasowych do API systemu.
   * @param roomsIds ID żądanych pokojów
   * @param periodIndex Indeks okienka czasowego w ciągu dnia
   * @param day Indeks dnia tygodnia
   * @param weeks Tygodnie, które należy wziąć pod uwagę
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę pokojów nie zawierających swoich nazw)
   */
  public GetRoomsAvailability(
    roomsIds:number[], 
    periodIndex:number, 
    day:number, 
    weeks:number[]):Observable<Room[]> {
      const request = {
        url: this.baseUrl + `/schedulePositions/Service.GetRoomsAvailability(RoomsIds=[${roomsIds.toString()}],` +
        `PeriodIndex=${periodIndex},Day=${day},Weeks=[${weeks.toString()}])`,
        method: 'GET'
      };
  
      return this.http.request(
        request.method,
        request.url,
        {
          headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
        }
      ).pipe(
        map((response : any) => response.value.map((element : any) => {
          const room = new Room(element.RoomId);
          room.IsBusy = element.IsBusy;
          return room;
        }))
      );
  }

  /**
   * Metoda wysyłająca żądanie przypisania nowego pokoju do przedmiotu do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param roomId ID żądanego pokoju
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public AddCourseRoom(courseId:number, roomId:number):Observable<any> {
    const request = {
      url: this.baseUrl + `/courseRooms`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          'CourseId': courseId,
          'RoomId': roomId
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o grupach do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę grup)
   */
  public GetGroups(): Observable<Group[]> {
    const request = {
      url: this.baseUrl + `/groups`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => {
        const group = new Group(element.GroupId);
        group.ParentGroupId = element.ParentGroupId;
        group.FullName = element.Name;
        return group;
      }))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o grupach, do których należy użytkownik do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę grup użytkownika)
   */
  public GetMyGroups(): Observable<Group[]> {
    const request = {
      url: this.baseUrl + `/studentGroups/Service.GetMyGroups()`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => {
        const group = new Group(element.GroupId);
        group.ParentGroupId = element.ParentGroupId;
        group.FullName = element.Name;
        return group;
      }))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania większej informacji o grupie do API systemu.
   * @param groupId Identyfikator żądanej grupy
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetGroupInfo(groupId: number): Observable<GroupInfo> {
    const request = {
      url: this.baseUrl + `/groups(${groupId})/Service.GetGroupFullInfo()`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new GroupInfo(
        response.GroupId, response.BasicName, response.FullName, response.ParentIds, response.ChildIds
      ))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania większej informacji o grupach do API systemu.
   * @param groupsIds Identyfikatory żądanych grup
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę większych informacji o grupach)
   */
  public GetGroupsInfo(groupsIds: number[]): Observable<GroupInfo[]> {
    const request = {
      url: this.baseUrl + `/groups/Service.GetGroupsFullInfo(GroupsIds=[${groupsIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new GroupInfo(
        element.GroupId, element.BasicName, element.FullName, element.ParentIds, element.ChildIds
      )))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania pełnych nazw grup do API systemu.
   * @param groupsIds Identyfikatory żądanych grup
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę pełnych nazw grup)
   */
  public GetGroupsFullNames(groupsIds:number[]):Observable<string[]> {
    const request = {
      url: this.baseUrl + `/groups/Service.GetGroupsFullNames(GroupsIds=[${groupsIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => element.FullName))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o zajętych ramach czasowych dla konkretnej edycji zajęć do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param courseEditionId ID żądanej edycji zajęć
   * @param weeks Tygodnie, które zostaną wzięte pod uwagę
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę pojedynczych zajętych miejsc w planie powodujących konflikty)
   */
  public GetBusyPeriods(courseId:number, courseEditionId:number, weeks:number[]):Observable<ScheduleSlot[]> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.GetBusyPeriods(Weeks=[${weeks.toString()}])?` +
      `$apply=groupby((PeriodIndex,Day))&$orderby=Day asc, PeriodIndex asc`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.map(
        (element : any) => new ScheduleSlot(element.PeriodIndex, element.Day)
      ))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji czy dana rama czasowa jest zajęta dla konkretnej edycji zajęć do API systemu.
   * @param courseId ID żądanego przedmiotu
   * @param courseEditionId ID żądanej edycji zajęć
   * @param periodIndex Indeks okienka czasowego w ciągu dnia
   * @param day Indeks dnia tygodnia
   * @param weeks Tygodnie, które zostaną wzięte pod uwagę
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public IsPeriodBusy(
    courseId:number, courseEditionId:number,
    periodIndex:number, day:number,
    weeks:number[]
  ):Observable<boolean> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.IsPeriodBusy(`
      + `PeriodIndex=${periodIndex},Day=${day},Weeks=[${weeks.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value)
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania większej ilości informacji na temat zaplanowanych ruchów do API systemu (wymaganych do wyświetlenia na ekranie).
   * @param movesIds Identyfikatory zaplanowanych ruchów w planie
   * @param roomsTypes Kolekcja typów pokojów
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę dokładniejszych informacji na temat zaplanowanych ruchów w systemie)
   */
  public GetConcreteScheduledMoves(movesIds:number[], roomsTypes:Map<number,RoomType>):Observable<ScheduledMoveDetails[]> {
    const request = {
      url: this.baseUrl + `/scheduledMoves/Service.GetConcreteScheduledMoves(MovesIds=[${movesIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((move : any) => {
        const room = new Room(move.DestRoomId);
        room.Name = move.DestRoomName;
        room.RoomType = roomsTypes.get(move.DestRoomTypeId) ?? new RoomType(0, "");
        return new ScheduledMoveDetails(
          move.MoveId,
          move.IsConfirmed,
          move.UserId,
          move.SourceWeeks,
          room,
          move.DestPeriodIndex,
          move.DestDay,
          move.DestWeeks);
      }))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania dodatkowych informacji na temat zaplanowanego ruchu do API systemu.
   * @param moveId Identyfikator zaplanowanego ruchu
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetScheduledMoveInfo(moveId: number):Observable<ScheduledMoveInfo> {
    const request = {
      url: this.baseUrl + `/scheduledMoves(${moveId})?$expand=User,Message`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new ScheduledMoveInfo(
        response.User.FirstName,
        response.User.LastName,
        new Titles(response.User.TitleBefore, response.User.TitleAfter),
        response.Message?.Content
      ))
    );
  }
}
