import { Injectable } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Period } from '../../projects/project-create/project-create.component';
import { HQService } from '../../services/hq.service';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import {
  BehaviorSubject,
  Observable,
  Subject,
  combineLatest,
  debounceTime,
  map,
  merge,
  shareReplay,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import {
  GetDashboardTimeV1ChargeCode,
  GetDashboardTimeV1Client,
  GetDashboardTimeV1Response,
} from '../../models/staff-dashboard/get-dashboard-time-v1';
import { TimeStatus } from '../../models/common/time-status';

@Injectable({
  providedIn: 'root',
})
export class StaffDashboardService {
  search = new FormControl<string | null>(null);
  period = new FormControl<Period>(Period.Today, { nonNullable: true });
  date = new FormControl<string>(
    new Date(new Date().getTime() - new Date().getTimezoneOffset() * 60000)
      .toISOString()
      .split('T')[0],
    {
      nonNullable: true,
    },
  );

  Period = Period;
  time$: Observable<GetDashboardTimeV1Response>;
  chargeCodes$: Observable<GetDashboardTimeV1ChargeCode[]>;
  clients$: Observable<GetDashboardTimeV1Client[]>;
  anyTimePending$: Observable<boolean>;

  refresh$ = new Subject<void>();

  constructor(
    private hqService: HQService,
    private oidcSecurityService: OidcSecurityService,
  ) {
    const staffId$ = oidcSecurityService.userData$.pipe(
      map((t) => t.userData),
      map((t) => t.staff_id as string),
    );

    const search$ = this.search.valueChanges.pipe(startWith(this.search.value));
    const period$ = this.period.valueChanges.pipe(startWith(this.period.value));
    const date$ = this.date.valueChanges
      .pipe(startWith(this.date.value))
      .pipe(
        map(
          (t) =>
            t ||
            new Date(
              new Date().getTime() - new Date().getTimezoneOffset() * 60000,
            )
              .toISOString()
              .split('T')[0],
        ),
      );

    const request$ = combineLatest({
      staffId: staffId$,
      period: period$,
      search: search$,
      date: date$,
    }).pipe(shareReplay(1));

    const time$ = request$.pipe(
      debounceTime(250),
      switchMap((request) => this.hqService.getDashboardTimeV1(request)),
      tap((response) =>
        this.date.setValue(response.startDate, { emitEvent: false }),
      ),
      map((response) => {
        const anyPending = response.dates.some((date) =>
          date.times.some((time) => time.timeStatus === TimeStatus.Pending),
        );
        return response;
      }),
    );
    const refreshTime$ = this.refresh$.pipe(switchMap((t) => time$));

    this.time$ = merge(time$, refreshTime$).pipe(shareReplay(1));

    this.anyTimePending$ = this.time$.pipe(
      map((response) =>
        response.dates.some((date) =>
          date.times.some((time) => time.timeStatus === TimeStatus.Pending),
        ),
      ),
    );

    this.chargeCodes$ = this.time$.pipe(map((t) => t.chargeCodes));
    this.clients$ = this.time$.pipe(map((t) => t.clients));
  }
  refresh() {
    this.refresh$.next();
  }
}