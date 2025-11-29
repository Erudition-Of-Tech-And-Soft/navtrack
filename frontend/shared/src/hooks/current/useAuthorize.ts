import { useCallback } from "react";
import { useCurrentUserQuery } from "../queries/user/useCurrentUserQuery";
import { AssetUserRole, OrganizationUserRole } from "../../api/model";
import { useCurrentOrganization } from "./useCurrentOrganization";
import { useCurrentAsset } from "./useCurrentAsset";

export function useAuthorize() {
  const currentUser = useCurrentUserQuery();
  const currentOrganization = useCurrentOrganization();
  const currentAsset = useCurrentAsset();

  const authorizeOrganization = useCallback(
    (userRole: OrganizationUserRole) => {
      const organization = currentUser.data?.organizations?.find(
        (x) => x.organizationId === currentOrganization.id
      );

      switch (userRole) {
        case OrganizationUserRole.Owner:
          return organization?.userRole === OrganizationUserRole.Owner;
        case OrganizationUserRole.Employee:
          return (
            organization?.userRole === OrganizationUserRole.Owner ||
            organization?.userRole === OrganizationUserRole.Employee
          );
        case OrganizationUserRole.Member:
          return (
            organization?.userRole === OrganizationUserRole.Owner ||
            organization?.userRole === OrganizationUserRole.Employee ||
            organization?.userRole === OrganizationUserRole.Member
          );
        case OrganizationUserRole.Seizer:
          return (
            organization?.userRole === OrganizationUserRole.Owner ||
            organization?.userRole === OrganizationUserRole.Employee ||
            organization?.userRole === OrganizationUserRole.Seizer
          );
        default:
          return false;
      }
    },
    [currentOrganization.id, currentUser.data?.organizations]
  );

  const assetAuthorize = useCallback(
    (userRole: AssetUserRole) => {
      const isOrganizationOwner = authorizeOrganization(
        OrganizationUserRole.Owner
      );

      if (isOrganizationOwner) {
        return true;
      }

      const asset = currentUser.data?.assets?.find(
        (x) => x.assetId === currentAsset.id
      );

      switch (userRole) {
        case AssetUserRole.Owner:
          return asset?.userRole === AssetUserRole.Owner;
        case AssetUserRole.Viewer:
          return (
            asset?.userRole === AssetUserRole.Owner ||
            asset?.userRole === AssetUserRole.Viewer
          );
        default:
          return false;
      }
    },
    [authorizeOrganization, currentAsset.id, currentUser.data?.assets]
  );

  const canEdit = useCallback(() => {
    return authorizeOrganization(OrganizationUserRole.Owner);
  }, [authorizeOrganization]);

  const canSendCommands = useCallback(() => {
    return authorizeOrganization(OrganizationUserRole.Employee);
  }, [authorizeOrganization]);

  const isSeizer = useCallback(() => {
    const organization = currentUser.data?.organizations?.find(
      (x) => x.organizationId === currentOrganization.id
    );
    return organization?.userRole === OrganizationUserRole.Seizer;
  }, [currentOrganization.id, currentUser.data?.organizations]);

  return {
    organization: authorizeOrganization,
    asset: assetAuthorize,
    canEdit,
    canSendCommands,
    isSeizer,
  };
}
