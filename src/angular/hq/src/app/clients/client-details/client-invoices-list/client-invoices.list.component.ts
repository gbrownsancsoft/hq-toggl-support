import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable, startWith, combineLatest, map, tap, of, debounceTime, switchMap, shareReplay } from 'rxjs';
import { SortColumn } from '../../../models/clients/get-client-v1';
import { SortDirection } from '../../../models/common/sort-direction';
import { HQService } from '../../../services/hq.service';
import { ClientDetailsService } from '../../client-details.service';
import { GetInvoicesRecordV1 } from '../../../models/Invoices/get-invoices-v1';
import { CommonModule } from '@angular/common';
import { PaginatorComponent } from '../../../common/paginator/paginator.component';

@Component({
  selector: 'hq-client-invoices-list',
  standalone: true,
  imports: [RouterLink, CommonModule, ReactiveFormsModule, PaginatorComponent],
  templateUrl: './client-invoices.component-list.html',
})
export class ClientInvoicesComponent {
  clientId?: string;
  invoices$: Observable<GetInvoicesRecordV1[]>;
  apiErrors: string[] = [];

  itemsPerPage = new FormControl(10, { nonNullable: true });

  page = new FormControl<number>(1, { nonNullable: true });

  skipDisplay$: Observable<number>;
  takeToDisplay$: Observable<number>;
  totalRecords$: Observable<number>;

  constructor(
    private hqService: HQService,
    private route: ActivatedRoute,
    private clientDetailService: ClientDetailsService
  ) {
    this.route.paramMap.subscribe((paramMap) => {
      this.clientId = paramMap.get('clientId') || undefined
      console.log(this.clientId); // Now clientId should have a value
    });
    const itemsPerPage$ = this.itemsPerPage.valueChanges.pipe(
      startWith(this.itemsPerPage.value)
    );
    const page$ = this.page.valueChanges.pipe(startWith(this.page.value));

    const skip$ = combineLatest([itemsPerPage$, page$]).pipe(
      map(([itemsPerPage, page]) => (page - 1) * itemsPerPage),
      startWith(0)
    );
    const search$ = clientDetailService.search.valueChanges.pipe(
      tap((t) => this.goToPage(1)),
      startWith(clientDetailService.search.value)
    );

    this.skipDisplay$ = skip$.pipe(map((skip) => skip + 1));

    const request$ = combineLatest({
      search: search$,
      skip: skip$,
      take: itemsPerPage$,
      sortBy: of(SortColumn.Name),
      sortDirection: of(SortDirection.Asc),
    });

    const response$ = request$.pipe(
      debounceTime(500),
      switchMap((request) => this.hqService.getInvoicesV1(request)),
      shareReplay(1)
    );

    this.invoices$ = response$.pipe(
      map((response) => {
        return response.records;
      })
    );

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

    this.clientDetailService.resetFilters();
    this.clientDetailService.hideProjectStatus();
    this.clientDetailService.hideCurrentOnly();
  }

  goToPage(page: number) {
    this.page.setValue(page);
  }
}
