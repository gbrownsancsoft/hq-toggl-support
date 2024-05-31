import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Observable,
  BehaviorSubject,
  startWith,
  combineLatest,
  map,
  tap,
  debounceTime,
  switchMap,
  shareReplay,
  first,
  firstValueFrom,
} from 'rxjs';
import {
  ClientDetailsService,
  ProjectStatus,
} from '../../clients/client-details.service';
import { GetPSRRecordV1, SortColumn } from '../../models/PSR/get-PSR-v1';
import { SortDirection } from '../../models/common/sort-direction';
import { HQService } from '../../services/hq.service';
import { CommonModule } from '@angular/common';
import { PaginatorComponent } from '../../common/paginator/paginator.component';
import { SortIconComponent } from '../../common/sort-icon/sort-icon.component';
import { Period } from '../../projects/project-create/project-create.component';
import { PsrSearchFilterComponent } from '../psr-search-filter/psr-search-filter.component';
import { PsrService } from '../psr-service';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'hq-psrlist',
  standalone: true,
  imports: [
    RouterLink,
    CommonModule,
    ReactiveFormsModule,
    PaginatorComponent,
    SortIconComponent,
    PsrSearchFilterComponent,
  ],
  templateUrl: './psrlist.component.html',
})
export class PSRListComponent implements OnInit, OnDestroy {
  apiErrors: string[] = [];

  skipDisplay$: Observable<number>;
  takeToDisplay$: Observable<number>;
  totalRecords$: Observable<number>;
  projectStatusReports$: Observable<GetPSRRecordV1[]>;
  sortOption$: BehaviorSubject<SortColumn>;
  sortDirection$: BehaviorSubject<SortDirection>;

  Math = Math;

  itemsPerPage = new FormControl(20, { nonNullable: true });
  page = new FormControl<number>(1, { nonNullable: true });

  sortColumn = SortColumn;
  sortDirection = SortDirection;

  async ngOnInit() {
    this.psrService.resetFilter();
    this.psrService.showSearch();
    this.psrService.showStaffMembers();
    this.psrService.showIsSubmitted();
    const staffId = await firstValueFrom(
      this.oidcSecurityService.userData$.pipe(map((t) => t.userData?.staff_id))
    );
    if (staffId) {
      this.psrService.staffMember.setValue(staffId);
    } else {
      console.log('ERROR: Could not find staff');
    }
  }
  ngOnDestroy(): void {
    this.psrService.resetFilter();
  }

  constructor(
    private hqService: HQService,
    private route: ActivatedRoute,
    private psrService: PsrService,
    private oidcSecurityService: OidcSecurityService
  ) {
    this.sortOption$ = new BehaviorSubject<SortColumn>(SortColumn.ChargeCode);
    this.sortDirection$ = new BehaviorSubject<SortDirection>(SortDirection.Asc);

    const itemsPerPage$ = this.itemsPerPage.valueChanges.pipe(
      startWith(this.itemsPerPage.value)
    );
    const page$ = this.page.valueChanges.pipe(startWith(this.page.value));

    const skip$ = combineLatest([itemsPerPage$, page$]).pipe(
      map(([itemsPerPage, page]) => (page - 1) * itemsPerPage),
      startWith(0)
    );
    const search$ = psrService.search.valueChanges.pipe(
      tap((t) => this.goToPage(1)),
      startWith(psrService.search.value)
    );

    this.skipDisplay$ = skip$.pipe(map((skip) => skip + 1));
    const staffMemberId$ = psrService.staffMember.valueChanges.pipe(
      startWith(psrService.staffMember.value)
    );
    const isSubmitted$ = psrService.isSubmitted.valueChanges.pipe(
      startWith(psrService.staffMember.value),
      map((value) => (value === null ? null : Boolean(value)))
    );

    const request$ = combineLatest({
      search: search$,
      skip: skip$,
      take: itemsPerPage$,
      sortBy: this.sortOption$,
      projectManagerId: staffMemberId$,
      sortDirection: this.sortDirection$,
      isSubmitted: isSubmitted$,
    });

    const response$ = request$.pipe(
      debounceTime(500),
      switchMap((request) => this.hqService.getPSRV1(request)),
      shareReplay(1)
    );

    const staffMembersResponse$ = this.hqService
      .getStaffMembersV1({ isAssignedProjectManager: true })
      .pipe(
        map((response) => response.records),
        map((records) =>
          records.map((record) => ({
            id: record.id,
            name: record.name,
            totalHours: record.workHours,
          }))
        )
      );

    staffMembersResponse$.pipe(first()).subscribe((response) => {
      psrService.staffMembers$.next(response);
    });

    this.projectStatusReports$ = response$.pipe(
      map((response) => {
        return response.records;
      })
    );
    // psrService.isSubmitted.valueChanges.subscribe((submitted) => {
    //   this.projectStatusReports$ = response$.pipe(
    //     map((response) => {
    //       return response.records;
    //     }),
    //     map((reports) =>
    //       reports.filter((report) => {
    //         console.log(report.submittedAt);
    //         if (submitted === null) {
    //           return true;
    //         } else if (submitted) {
    //           return report.submittedAt !== null;
    //         } else {
    //           return report.submittedAt === null;
    //         }
    //       })
    //     )
    //   );
    // });

    this.totalRecords$ = response$.pipe(map((t) => t.total!));

    this.takeToDisplay$ = combineLatest([
      skip$,
      itemsPerPage$,
      this.totalRecords$,
    ]).pipe(
      map(([skip, itemsPerPage, totalRecords]) =>
        Math.min(skip + itemsPerPage, totalRecords)
      )
    );

    this.psrService.resetFilter();
  }

  goToPage(page: number) {
    this.page.setValue(page);
  }

  onSortClick(sortColumn: SortColumn) {
    if (this.sortOption$.value == sortColumn) {
      this.sortDirection$.next(
        this.sortDirection$.value == SortDirection.Asc
          ? SortDirection.Desc
          : SortDirection.Asc
      );
    } else {
      this.sortOption$.next(sortColumn);
      this.sortDirection$.next(SortDirection.Asc);
    }
    this.page.setValue(1);
  }
  getProjectSatusString(status: ProjectStatus): string {
    return ProjectStatus[status];
  }

  getPeriodName(period: Period) {
    return Period[period];
  }
}
