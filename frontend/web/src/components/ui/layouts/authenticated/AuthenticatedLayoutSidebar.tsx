import { Copyright } from "../../../shared/Copyright";
import { FormattedMessage } from "react-intl";
import { generatePath, Link } from "react-router-dom";
import { Paths } from "../../../../app/Paths";
import { NavtrackLogoDark } from "../../logo/NavtrackLogoDark";
import { useCurrentOrganization } from "@navtrack/shared/hooks/current/useCurrentOrganization";
import { useAssetsQuery } from "@navtrack/shared/hooks/queries/assets/useAssetsQuery";
import { LoadingIndicator } from "../../loading-indicator/LoadingIndicator";
import { AuthenticatedLayoutSidebarItem } from "./AuthenticatedLayoutSidebarItem";
import { Button } from "../../button/Button";
import { faPlus } from "@fortawesome/free-solid-svg-icons";
import { Icon } from "../../icon/Icon";
import { faHdd } from "@fortawesome/free-regular-svg-icons";
import { useMemo, useState } from "react";
import { Authorize } from "@navtrack/shared/components/authorize/Authorize";
import { OrganizationUserRole } from "@navtrack/shared/api/model";

export function AuthenticatedLayoutSidebar() {
  const currentOrganization = useCurrentOrganization();
  const assetsQuery = useAssetsQuery({
    organizationId: currentOrganization.data?.id
  });
  const [searchTerm, setSearchTerm] = useState("");

  const logoPath = useMemo(() => {
    if (currentOrganization.id) {
      return generatePath(Paths.OrganizationLive, {
        id: currentOrganization.id
      });
    }

    return Paths.Home;
  }, [currentOrganization.id]);

  const filteredAssets = useMemo(() => {
    if (!assetsQuery.data?.items) return [];

    if (!searchTerm.trim()) {
      return assetsQuery.data.items;
    }

    const searchLower = searchTerm.toLowerCase().trim();
    return assetsQuery.data.items.filter((asset) =>
      asset.name.toLowerCase().includes(searchLower)
    );
  }, [assetsQuery.data?.items, searchTerm]);

  return (
    <div className="absolute bottom-0 top-0 flex w-64 flex-col">
      <div className="relative flex h-14 items-center bg-gray-900 px-4">
        <Link to={logoPath} className="flex items-center">
          <NavtrackLogoDark className="h-10 w-10 p-2" />
          <span className="ml-2 text-2xl font-semibold tracking-wide text-white">
            <FormattedMessage id="navtrack" />
          </span>
        </Link>
      </div>
      <div className="flex h-14 items-center justify-between bg-gray-800 px-4 text-xs font-medium uppercase tracking-wider text-white">
        <Link
          to={generatePath(Paths.OrganizationAssets, {
            id: `${currentOrganization.id}`
          })}>
          <Icon icon={faHdd} className="mr-2" />
          <FormattedMessage id="generic.assets" />
        </Link>
        <Authorize organizationUserRole={OrganizationUserRole.Owner}>
          <Link
            to={generatePath(Paths.OrganizationAssetsNew, {
              id: `${currentOrganization.id}`
            })}>
            <Button size="xs" color="success" icon={faPlus}>
              <FormattedMessage id="generic.new-asset" />
            </Button>
          </Link>
        </Authorize>
      </div>
      <div className="bg-gray-800 px-4 py-2">
        <input
          type="text"
          placeholder="Search assets..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-sm text-white placeholder-gray-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
        />
      </div>
      <div
        className="relative flex-1 overflow-y-scroll bg-gray-800 py-2"
        style={{
          boxShadow:
            "inset 0 7px 9px -7px rgba(17,24,39,0.4), inset 0 -7px 9px -7px rgba(17,24,39,0.4)"
        }}>
        <div className="flex flex-1 flex-col space-y-1 px-2">
          {assetsQuery.isLoading ? (
            <LoadingIndicator className="mt-2 text-gray-300" size="lg" />
          ) : (
            <>
              {filteredAssets.length ? (
                filteredAssets.map((asset) => (
                  <AuthenticatedLayoutSidebarItem
                    key={asset.id}
                    asset={asset}
                  />
                ))
              ) : searchTerm ? (
                <div className="text-center text-sm text-white">
                  <FormattedMessage id="sidebar.no-results" />
                </div>
              ) : (
                <div className="text-center text-sm text-white">
                  <FormattedMessage id="sidebar.no-assets" />
                </div>
              )}
            </>
          )}
        </div>
      </div>
      <div className="flex h-12 items-center justify-center bg-gray-800 text-xs text-white">
        <Copyright />
      </div>
    </div>
  );
}
