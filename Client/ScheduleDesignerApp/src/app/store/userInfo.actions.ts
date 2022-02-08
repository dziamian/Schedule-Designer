import { createAction, props } from '@ngrx/store'
import { UserInfo } from '../others/Accounts';

export const setUserInfo = createAction(
    'SetUserInfo',
    props<{userInfo:UserInfo}>()
);