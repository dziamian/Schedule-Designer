import { createAction, props } from '@ngrx/store'
import { Account } from '../others/Accounts';

export const setAccount = createAction(
    'SetAccount',
    props<{account:Account}>()
);